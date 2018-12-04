#include "YBehavior/debugger.h"
#include "YBehavior/agent.h"
#include "YBehavior/tools/objectpool.h"
#include "YBehavior/network/network.h"
#include "YBehavior/logger.h"
#include "YBehavior/interface.h"
#include "YBehavior/nodefactory.h"
#include <sstream>
#include "YBehavior/shareddataex.h"
#include "YBehavior/memory.h"

#ifdef DEBUGGER
namespace YBehavior
{
	const STRING NodeRunInfo::ToString() const
	{
		STRING str;
		std::stringstream ss;
		ss << nodeUID << '=' << (INT)runState;
		ss >> str;
		return str;
	}

	DebugMgr::~DebugMgr()
	{
		Clear();
	}

	void DebugMgr::SetTarget(const STRING& tree, UINT agent)
	{
		m_TargetTree = tree;
		//m_TargetHash = hash;
		m_TargetAgent = agent;
		m_bTargetDirty = true;
	}

	void DebugMgr::ResetTarget()
	{
		SetTarget(Utility::StringEmpty, 0);
	}

	void DebugMgr::Stop()
	{
		ResetTarget();
		Clear();
		m_SendBuffer = "";
		if (m_bPaused)
		{
			m_bPaused = false;
			m_Command = DC_Continue;
		}
		else
			m_Command = DC_None;
	}

	bool DebugMgr::IsValidTarget(Agent* pAgent, BehaviorTree* pTree)
	{
		if (pAgent == nullptr)
			return false;

		if (m_bTargetDirty)
		{
			if (m_TargetAgent == 0 || m_TargetAgent == pAgent->GetDebugUID())
			{
				if (pAgent->GetTree()->GetTreeName() == m_TargetTree)
				{
					std::list<BehaviorTree*> toVisit;
					std::unordered_set<BehaviorTree*> visited;

					toVisit.push_back(pAgent->GetTree());
					visited.insert(pAgent->GetTree());
					while (!toVisit.empty())
					{
						BehaviorTree* pCurTree = toVisit.front();
						toVisit.pop_front();

						auto it = m_TreeDebugInfo.find(pCurTree->GetTreeName());
						if (it == m_TreeDebugInfo.end())
						{
							if (m_TargetAgent != 0)
							{
								LOG_BEGIN << "Agent " << m_TargetAgent << " is running " << pCurTree->GetTreeName() << " but there's no DebugInfo." << LOG_END;
								m_TargetAgent = 0;
							}
							return false;
						}
						if (it->second.Hash == pCurTree->GetHash())
						{
							for (auto it2 : pCurTree->GetSubTrees())
							{
								if (visited.count(it2) == 0)
								{
									toVisit.push_back(it2);
									visited.insert(it2);
								}
							}
						}
						else
						{
							LOG_BEGIN << "Agent " << pAgent->GetDebugUID() <<"with tree " << pCurTree->GetTreeName() << " has diffrent VERSION of tree with Editor" << LOG_END;
							return false;
						}
					}

					m_bTargetDirty = false;
					m_TargetAgent = pAgent->GetDebugUID();
					return true;
				}
				else
				{
					if (m_TargetAgent != 0)
					{
						LOG_BEGIN << "Agent " << m_TargetAgent << " is running " << pAgent->GetTree()->GetTreeName() << " instead of " << m_TargetTree << LOG_END;
					}
					//m_TargetAgent = 0;
					return false;
				}
			}
			else
			{
				return false;
			}
		}
		else
		{
			return m_TargetAgent == pAgent->GetDebugUID();
		}
	}

	bool DebugMgr::HasBreakPoint(const STRING& treeName, UINT nodeUID)
	{
		auto it = m_TreeDebugInfo.find(treeName);
		if (it == m_TreeDebugInfo.end())
			return false;
		auto it2 = it->second.DebugPointInfos.find(nodeUID);
		if (it2 == it->second.DebugPointInfos.end())
			return false;
		return it2->second.HasBreakPoint();
	}

	bool DebugMgr::HasLogPoint(const STRING& treeName, UINT nodeUID)
	{
		auto it = m_TreeDebugInfo.find(treeName);
		if (it == m_TreeDebugInfo.end())
			return false;
		auto it2 = it->second.DebugPointInfos.find(nodeUID);
		if (it2 == it->second.DebugPointInfos.end())
			return false;
		return it2->second.HasLogPoint();
	}

	bool DebugMgr::HasDebugPoint(const STRING& treeName, UINT nodeUID)
	{
		auto it = m_TreeDebugInfo.find(treeName);
		if (it == m_TreeDebugInfo.end())
			return false;
		auto it2 = it->second.DebugPointInfos.find(nodeUID);
		if (it2 == it->second.DebugPointInfos.end())
			return false;
		return !it2->second.NoDebugPoint();
	}

	void DebugMgr::AddBreakPoint(const STRING& treeName, UINT nodeUID)
	{
		auto it = m_TreeDebugInfo.find(treeName);
		if (it == m_TreeDebugInfo.end())
			return;
		DebugPointInfo info;
		info.nodeUID = nodeUID;
		info.count = 1;
		it->second.DebugPointInfos[nodeUID] = info;
	}

	void DebugMgr::AddLogPoint(const STRING& treeName, UINT nodeUID)
	{
		auto it = m_TreeDebugInfo.find(treeName);
		if (it == m_TreeDebugInfo.end())
			return;
		DebugPointInfo info;
		info.nodeUID = nodeUID;
		info.count = -1;
		it->second.DebugPointInfos[nodeUID] = info;
	}

	void DebugMgr::AddTreeDebugInfo(STRING&& name, TreeDebugInfo&& info)
	{
		m_TreeDebugInfo[name] = info;
	}

	void DebugMgr::RemoveDebugPoint(const STRING& treeName, UINT nodeUID)
	{
		auto it = m_TreeDebugInfo.find(treeName);
		if (it == m_TreeDebugInfo.end())
			return;

		it->second.DebugPointInfos.erase(nodeUID);
	}

	NodeRunInfo* DebugMgr::CreateAndAppendRunInfo()
	{
		NodeRunInfo* pInfo = ObjectPool<NodeRunInfo>::Get();
		m_RunInfos.push_back(pInfo);
		return pInfo;
	}

	void DebugMgr::Clear()
	{
		for (auto it = m_RunInfos.begin(); it != m_RunInfos.end(); ++it)
		{
			ObjectPool<NodeRunInfo>::Recycle(*it);
		}
		m_RunInfos.clear();
	}

	void DebugMgr::Send(bool bClearRunInfo)
	{
		if (m_SendBuffer.length() == 0)
			return;

		Network::Instance()->SendText(m_SendBuffer);
		m_SendBuffer = "";
		if (bClearRunInfo)
			Clear();
	}

	/////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////

	unsigned DebugHelper::s_Token = 0;

	DebugHelper::DebugHelper(Agent* pAgent, BehaviorNode* pNode)
		: m_pLogInfo(nullptr)
	{
		if (pAgent == nullptr || !DebugMgr::Instance()->IsValidTarget(pAgent, pNode->GetRoot()))
		{
			m_Target = nullptr;
			return;
		}

		m_Target = pAgent;
		m_pNode = pNode;

		CreateRunInfo();
		m_pRunInfo->nodeUID = pNode->GetUID();
		m_pRunInfo->runState = NS_RUNNING;
		m_pRunInfo->tree = pNode->GetRoot();

		if (DebugMgr::Instance()->HasDebugPoint(pNode->GetRoot()->GetTreeName(), pNode->GetUID()))
		{
			m_pLogInfo = ObjectPool<NodeLogInfo>::Get();
			m_pLogInfo->Reset();
			pNode->GetDebugLogInfo().str("");
		}

		m_Token = ++s_Token;
	}

	DebugHelper::~DebugHelper()
	{
		if (IsValid())
		{
			_SendLogPoint();

			///> Root node, send shared variables.
			if (m_pNode->GetParent() == nullptr)
			{
				BehaviorTree* tree = dynamic_cast<BehaviorTree*>(m_pNode);
				if (tree && tree->GetTreeName() == DebugMgr::Instance()->GetTargetTree())
					_SendInfos(true);
			}

			if (this == DebugMgr::Instance()->GetStepOverHelper())
			{
				DebugMgr::Instance()->SetStepOverHelper(nullptr);
				DebugMgr::Instance()->TogglePause(true);
			}
		}

		if (m_pLogInfo)
			ObjectPool<NodeLogInfo>::Recycle(m_pLogInfo);
	}

	void DebugHelper::_SendLogPoint()
	{
		if (m_pLogInfo == nullptr)
			return;

		m_pLogInfo->otherInfo = m_pNode->GetDebugLogInfo().str();
		m_pNode->GetDebugLogInfo().str("");

		DebugMgr::Instance()->AppendSendContent("[LogPoint]");
		DebugMgr::Instance()->AppendSendContent(s_HeadSpliter);
		DebugMgr::Instance()->AppendSendContent(((BehaviorTree*)m_pNode->GetRoot())->GetTreeName());
		DebugMgr::Instance()->AppendSendContent(" ");
		DebugMgr::Instance()->AppendSendContent(Utility::ToString(m_pNode->GetUID()));
		DebugMgr::Instance()->AppendSendContent(".");
		DebugMgr::Instance()->AppendSendContent(m_pNode->GetClassName());
		DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);

		if (m_pLogInfo->beforeInfo.size() > 0)
		{
			DebugMgr::Instance()->AppendSendContent("BEFORE");
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			DebugMgr::Instance()->AppendSendContent(Utility::ToString(m_pLogInfo->beforeInfo.size()));
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);

			for (auto it = m_pLogInfo->beforeInfo.begin(); it != m_pLogInfo->beforeInfo.end(); ++it)
			{
				DebugMgr::Instance()->AppendSendContent(*it);
				DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			}
		}

		if (m_pLogInfo->afterInfo.size() > 0)
		{
			DebugMgr::Instance()->AppendSendContent("AFTER");
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			DebugMgr::Instance()->AppendSendContent(Utility::ToString(m_pLogInfo->afterInfo.size()));
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);

			for (auto it = m_pLogInfo->afterInfo.begin(); it != m_pLogInfo->afterInfo.end(); ++it)
			{
				DebugMgr::Instance()->AppendSendContent(*it);
				DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			}
		}

		if (m_pLogInfo->otherInfo.size() > 0)
			DebugMgr::Instance()->AppendSendContent(m_pLogInfo->otherInfo);

		DebugMgr::Instance()->Send(false);
	}

	void DebugHelper::_SendCurrentInfos()
	{
		if (!IsValid())
			return;

		BehaviorNode* pRoot = m_pNode->GetRoot();
		if (pRoot)
		{
			BehaviorTree* tree = dynamic_cast<BehaviorTree*>(pRoot);
			if (tree)
				_SendInfos(false);
		}

	}

	void _OutputMemoryInfo(SharedDataEx* pSharedData, STRING& buffer)
	{
		if (pSharedData == nullptr)
			return;

		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			auto iarray = pSharedData->GetDataArray(i);
			for (IDataArray::Iterator it = iarray->Iter(); !it.IsEnd(); ++it)
			{
				const STRING& name = TreeKeyMgr::Instance()->GetNameByKey(it.Value(), iarray->TypeID());
				if (name == Utility::StringEmpty)
					continue;
				STRING content(name + "," + iarray->GetToString(it.Value()));
				if (buffer.length() > 0)
					buffer += ";";
				buffer += content;
			}
		}
	}

	void DebugHelper::_SendInfos(bool clear)
	{
		DebugMgr::Instance()->AppendSendContent("[TickResult]");
		DebugMgr::Instance()->AppendSendContent(s_HeadSpliter);

		///> SharedDatas:

		///> MainData
		{
			STRING buffer;
			SharedDataEx* pSharedData = m_Target->GetMemory()->GetMainData();

			_OutputMemoryInfo(pSharedData, buffer);
			DebugMgr::Instance()->AppendSendContent(buffer);
		}


		std::unordered_map<BehaviorTree*, std::pair<STRING, STRING>> treeBuffer;

		///> LocalData
		{
			MemoryStack& localStack = m_Target->GetMemory()->GetStack();
			for (auto it = localStack.rbegin(); it != localStack.rend(); ++it)
			{
				STRING& buf = treeBuffer[it->Owner].first;
				///> this node has run twice or more, only use the deepest data
				if (buf.length() > 0)
					continue;
				_OutputMemoryInfo(it->Data, buf);
			}
		}

		///> Run Info:
		const std::list<NodeRunInfo*>& runInfos = DebugMgr::Instance()->GetRunInfos();
		for (auto it = runInfos.begin(); it != runInfos.end(); ++it)
		{
			STRING& buf = treeBuffer[(*it)->tree].second;
			if (buf.length() > 0)
				buf += ";";
			buf += (*it)->ToString();
		}
		for (auto it = treeBuffer.begin(); it != treeBuffer.end(); ++it)
		{
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			DebugMgr::Instance()->AppendSendContent(it->first->GetTreeName());
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			DebugMgr::Instance()->AppendSendContent(it->second.first);
			DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
			DebugMgr::Instance()->AppendSendContent(it->second.second);
		}

		//if (DebugMgr::Instance()->IsPaused())
		//	LOG_BEGIN << "Dont Clear Run Info cause paused" << LOG_END;
		DebugMgr::Instance()->Send(clear);
	}

	void DebugHelper::CreateRunInfo()
	{
		m_pRunInfo = DebugMgr::Instance()->CreateAndAppendRunInfo();
	}

	void DebugHelper::SetResult(NodeState state)
	{
		if (IsValid())
			m_pRunInfo->runState = state;
	}

	void DebugHelper::TestBreaking()
	{
		if (!IsValid())
			return;

		if (DebugMgr::Instance()->IsPaused())
			Breaking();
		else if (DebugMgr::Instance()->HasBreakPoint(m_pNode->GetRoot()->GetTreeName(), m_pRunInfo->nodeUID))
			SetBreak();
	}

	void DebugHelper::TestPause()
	{
		if (!IsValid())
			return;

		///> No sub nodes have run. No need to pause
		if (m_Token == s_Token)
			return;

		if (DebugMgr::Instance()->IsPaused())
			Breaking();
	}

	bool DebugHelper::HasLogPoint()
	{
		if (!IsValid())
			return false;
		return DebugMgr::Instance()->HasLogPoint(m_pNode->GetRoot()->GetTreeName(), m_pRunInfo->nodeUID);
	}

	bool DebugHelper::HasDebugPoint()
	{
		if (!IsValid())
			return false;
		return DebugMgr::Instance()->HasDebugPoint(m_pNode->GetRoot()->GetTreeName(), m_pRunInfo->nodeUID);
	}

	void DebugHelper::SetBreak()
	{
		///> Send runInfos to editor
		DebugMgr::Instance()->TogglePause(true);
		DebugMgr::Instance()->SetStepOverHelper(nullptr);
		LOG_BEGIN << "Breaking..." << LOG_END;
		m_pRunInfo->runState = NS_BREAK;
		SetResult(NS_BREAK);

		Breaking();
	}

	void DebugHelper::Breaking()
	{
		_SendPause();
		_SendCurrentInfos();
		///> Sleep and wait for removing break
		while (DebugMgr::Instance()->GetCommand() == DC_None)
		{
			Thread::SleepMilli(100);
		}
		switch (DebugMgr::Instance()->GetCommand())
		{
		case DC_Continue:
			DebugMgr::Instance()->TogglePause(false);
			break;
		case DC_StepOver:
			DebugMgr::Instance()->TogglePause(false);
			DebugMgr::Instance()->SetStepOverHelper(this);
			break;
		default:
			break;
		}
		DebugMgr::Instance()->SetCommand(DC_None);
		m_pRunInfo->runState = NS_RUNNING;
		LOG_BEGIN << "Continue." << LOG_END;
	}

	void DebugHelper::_SendPause()
	{
		DebugMgr::Instance()->AppendSendContent("[Paused]");
		DebugMgr::Instance()->Send(false);
	}

	void DebugHelper::LogSharedData(ISharedVariableEx* pVariable, bool bBefore)
	{
		if (!IsValid() || m_pLogInfo == nullptr || pVariable == nullptr)
			return;

		std::stringstream ss;
		ss << pVariable->GetName() << " ";

		///>  Like:      Int0 <CONST> 44
		if (pVariable->IsConst())
			ss << "<CONST> " << pVariable->GetValueToSTRING(m_Target->GetMemory());
		else
		{
			ISharedVariableEx* pVectorIndex = pVariable->GetVectorIndex();
			const STRING& sharedDataVariableName = TreeKeyMgr::Instance()->GetNameByKey(pVariable->GetKey(), pVariable->GetReferenceSharedDataSelfID());
			///>  Like:      Int0 IntArrayM[7] 44
			if (pVectorIndex)
			{

				ss << sharedDataVariableName << "[" << pVectorIndex->GetValueToSTRING(m_Target->GetMemory()) << "] ";
			}
			///>  Like:      Int0 IntC 4
			else
			{
				ss << sharedDataVariableName << " ";
			}
			ss << pVariable->GetValueToSTRING(m_Target->GetMemory());
		}

		STRING res(ss.str());
		if (bBefore)
			m_pLogInfo->beforeInfo.push_back(res);
		else
			m_pLogInfo->afterInfo.push_back(res);
	}
}
#endif // DEBUGGER
