#include "YBehavior/debugger.h"
#include "YBehavior/agent.h"
#include "YBehavior/tools/objectpool.h"
#include "YBehavior/network/network.h"
#include "YBehavior/logger.h"
#include "YBehavior/interface.h"
#include "YBehavior/nodefactory.h"
#include <sstream>

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

	void DebugMgr::SetTarget(const STRING& tree, UINT agent)
	{
		m_TargetTree = tree;
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
		m_bPaused = false;
		m_SendBuffer = "";
	}

	bool DebugMgr::IsValidTarget(Agent* pAgent)
	{
		if (pAgent == nullptr)
			return false;

		if (m_bTargetDirty)
		{
			if (m_TargetAgent == 0 || m_TargetAgent == pAgent->GetUID())
			{
				if (pAgent->GetTree()->GetTreeName() == m_TargetTree)
				{
					m_bTargetDirty = false;
					m_TargetAgent = pAgent->GetUID();
					return true;
				}
				m_TargetAgent = 0;
				return false;
			}
			else
			{
				return false;
			}
		}
		else
		{
			return m_TargetAgent == pAgent->GetUID();
		}
	}

	bool DebugMgr::HasBreakPoint(UINT nodeUID)
	{
		auto it = m_DebugPointInfos.find(nodeUID);
		if (it == m_DebugPointInfos.end())
			return false;
		return it->second.HasBreakPoint();
	}

	bool DebugMgr::HasLogPoint(UINT nodeUID)
	{
		auto it = m_DebugPointInfos.find(nodeUID);
		if (it == m_DebugPointInfos.end())
			return false;
		return it->second.HasLogPoint();
	}

	void DebugMgr::AddBreakPoint(UINT nodeUID)
	{
		DebugPointInfo info;
		info.nodeUID = nodeUID;
		info.count = 1;
		m_DebugPointInfos[nodeUID] = info;
	}

	void DebugMgr::AddLogPoint(UINT nodeUID)
	{
		DebugPointInfo info;
		info.nodeUID = nodeUID;
		info.count = -1;
		m_DebugPointInfos[nodeUID] = info;
	}

	void DebugMgr::RemoveDebugPoint(UINT nodeUID)
	{
		m_DebugPointInfos.erase(nodeUID);
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


	DebugHelper::DebugHelper(Agent* pAgent, BehaviorNode* pNode)
		: m_pLogInfo(nullptr)
	{
		if (pAgent == nullptr || !DebugMgr::Instance()->IsValidTarget(pAgent))
		{
			m_Target = nullptr;
			return;
		}

		m_Target = pAgent;
		m_pNode = pNode;

		CreateRunInfo();
		m_pRunInfo->nodeUID = pNode->GetUID();
		m_pRunInfo->runState = NS_RUNNING;

		if (DebugMgr::Instance()->HasLogPoint(pNode->GetUID()))
		{
			m_pLogInfo = new NodeLogInfo();
			pNode->GetDebugLogInfo().str("");
		}
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
				if (tree)
					_SendInfos();
			}
		}

		if (m_pLogInfo)
			delete m_pLogInfo;
	}

	void DebugHelper::_SendLogPoint()
	{
		if (m_pLogInfo == nullptr)
			return;

		m_pLogInfo->otherInfo = m_pNode->GetDebugLogInfo().str();
		m_pNode->GetDebugLogInfo().str("");

		DebugMgr::Instance()->AppendSendContent("[LogPoint]");
		DebugMgr::Instance()->AppendSendContent(s_HeadSpliter);
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
				_SendInfos();
		}

	}

	void DebugHelper::_SendInfos()
	{
		DebugMgr::Instance()->AppendSendContent("[TickResult]");
		DebugMgr::Instance()->AppendSendContent(s_HeadSpliter);

		///> SharedDatas:
		STRING buffer;
		SharedDataEx* pSharedData = m_Target->GetSharedData();

		const STRING& treeName = DebugMgr::Instance()->GetTargetTree();

		for (int i = 0; i < MAX_TYPE_INDEX; ++i)
		{
			auto iarray = pSharedData->GetDataArray(i);
			int length = iarray->Length();
			for (int j = 0; j < length; ++j)
			{
				const STRING& name = NodeFactory::Instance()->GetNameByIndex(treeName, j, iarray->GetTypeID());
				if (name == Utility::StringEmpty)
					continue;
				STRING content(name + "," + iarray->GetToString(j));
				if (buffer.length() > 0)
					buffer += ";";
				buffer += content;
			}
		}
		DebugMgr::Instance()->AppendSendContent(buffer);

		DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);

		///> Run Info:

		buffer = "";
		const std::list<NodeRunInfo*>& runInfos = DebugMgr::Instance()->GetRunInfos();
		for (auto it = runInfos.begin(); it != runInfos.end(); ++it)
		{
			if (buffer.length() > 0)
				buffer += ";";
			buffer += (*it)->ToString();
		}
		DebugMgr::Instance()->AppendSendContent(buffer);

		//if (DebugMgr::Instance()->IsPaused())
		//	LOG_BEGIN << "Dont Clear Run Info cause paused" << LOG_END;
		DebugMgr::Instance()->Send(!DebugMgr::Instance()->IsPaused());
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

	void DebugHelper::TryHitBreakPoint()
	{
		if (!IsValid())
			return;
		if (DebugMgr::Instance()->HasBreakPoint(m_pRunInfo->nodeUID))
			Breaking();
	}

	bool DebugHelper::HasLogPoint()
	{
		if (!IsValid())
			return false;
		return DebugMgr::Instance()->HasLogPoint(m_pRunInfo->nodeUID);
	}

	void DebugHelper::Breaking()
	{
		LOG_BEGIN << "Breaking..." << LOG_END;
		m_pRunInfo->runState = NS_BREAK;

		///> Send runInfos to editor
		DebugMgr::Instance()->TogglePause(true);

		SetResult(NS_BREAK);
		_SendPause();
		_SendCurrentInfos();
		///> Sleep and wait for removing break
		int i = 0;
		while (DebugMgr::Instance()->IsPaused())
		{
			Thread::SleepMilli(100);
			//if (++i > 10)
			//	DebugMgr::Instance()->TogglePause(false);
		}

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
			ss << "<CONST> " << pVariable->GetValueToSTRING(m_Target->GetSharedData());
		else
		{
			ISharedVariableEx* pVectorIndex = pVariable->GetVectorIndex();
			const STRING& sharedDataVariableName = NodeFactory::Instance()->GetNameByIndex(DebugMgr::Instance()->GetTargetTree(), pVariable->GetIndex(), pVariable->GetReferenceSharedDataSelfID());
			///>  Like:      Int0 IntArrayM[7] 44
			if (pVectorIndex)
			{

				ss << sharedDataVariableName << "[" << pVectorIndex->GetValueToSTRING(m_Target->GetSharedData()) << "] ";
			}
			///>  Like:      Int0 IntC 4
			else
			{
				ss << sharedDataVariableName << " ";
			}
			ss << pVariable->GetValueToSTRING(m_Target->GetSharedData());
		}

		STRING res(ss.str());
		if (bBefore)
			m_pLogInfo->beforeInfo.push_back(res);
		else
			m_pLogInfo->afterInfo.push_back(res);
	}
}
#endif // DEBUGGER
