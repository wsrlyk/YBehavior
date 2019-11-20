#include "YBehavior/debugger.h"
#include "YBehavior/agent.h"
#include "YBehavior/tools/objectpool.h"
#include "YBehavior/logger.h"
#include "YBehavior/interface.h"
#include "YBehavior/nodefactory.h"
#include <sstream>
#include "YBehavior/shareddataex.h"
#include "YBehavior/memory.h"
#include "YBehavior/fsm/machinetreemapping.h"

#ifdef DEBUGGER
namespace YBehavior
{
	const STRING NodeRunInfo::ToString() const
	{
		STRING str;
		std::stringstream ss;
		ss << nodeUID << IDebugHelper::s_SequenceSpliter << finalRunState;
		ss >> str;
		return str;
	}

	DebugMgr::~DebugMgr()
	{
		Clear();
	}

	void DebugMgr::SetTarget(const DebugTargetID& target)
	{
		m_Target = target;
		//m_TargetHash = hash;
		m_TargetAgent = 0;
		m_bTargetDirty = true;
		m_TryTarget = 0;
	}

	void DebugMgr::SetTarget(UINT64 target)
	{
		m_Target.Type = DebugTargetType::INVALID;
		//m_TargetHash = hash;
		m_TargetAgent = 0;
		m_bTargetDirty = true;
		m_TryTarget = target;
	}

	void DebugMgr::Begin()
	{
		if (m_TryTarget != 0 && !m_bTargetDirty)
		{
			m_TargetAgent = m_TryTarget;
		}
	}

	void DebugMgr::ResetTarget()
	{
		SetTarget(0);
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

		if (m_TargetAgent != 0)
		{
			return m_TargetAgent == pAgent->GetDebugUID();
		}
		else
		{
			if (m_bTargetDirty)
			{
				if ((m_TryTarget == 0
					&& m_Target.Type == DebugTargetType::TREE
					&& pTree->GetTreeName() == m_Target.Name)
					||
					m_TryTarget == pAgent->GetDebugUID())
				{
					_TryDebug(pAgent);
				}
			}
			else
				return false;
		}

		return false;
	}

	bool DebugMgr::IsValidTarget(Agent* pAgent, FSM* pFSM)
	{
		if (pAgent == nullptr)
			return false;

		if (m_TargetAgent != 0)
		{
			return m_TargetAgent == pAgent->GetDebugUID();
		}
		else
		{
			if (m_bTargetDirty)
			{
				if ((m_TryTarget == 0
					&& m_Target.Type == DebugTargetType::FSM
					&& pFSM->GetName() == m_Target.Name)
					||
					m_TryTarget == pAgent->GetDebugUID())
				{
					_TryDebug(pAgent);
				}
			}
			else
				return false;
		}

		return false;
	}

	void DebugMgr::_TryDebug(AgentPtr pAgent)
	{
		STRING buffer("[SubTrees]");
		buffer += IDebugHelper::s_HeadSpliter;

		///> Machine Name
		auto mapping = pAgent->GetMachineContext()->GetMapping();
		buffer += mapping->GetFSM()->GetNameWithPath();
		buffer += IDebugHelper::s_SequenceSpliter;
		buffer += Utility::ToString(mapping->GetFSM()->GetHash());
		//buffer += IDebugHelper::s_ListSpliter;

		///> Tree&SubTree Name
		std::list<BehaviorTree*> toVisit;
		std::unordered_set<BehaviorTree*> visited;

		for (const auto& it : mapping->GetMapping())
		{
			toVisit.push_back(it.second);
			visited.insert(it.second);
		}
		while (!toVisit.empty())
		{
			BehaviorTree* pCurTree = toVisit.front();
			toVisit.pop_front();

			for (auto it : pCurTree->GetSubTrees())
			{
				if (visited.count(it) == 0)
				{
					toVisit.push_back(it);
					visited.insert(it);
				}
			}
		}
		//bool bFirst = true;
		for (auto it : visited)
		{
			//if (bFirst)
			//{
			//	bFirst = false;
			//}
			//else
			{
				buffer += IDebugHelper::s_ListSpliter;
			}
			buffer += it->GetTreeNameWithPath();
			buffer += IDebugHelper::s_SequenceSpliter;
			buffer += Utility::ToString(it->GetHash());
		}
		Network::Instance()->SendText(buffer);

		m_bTargetDirty = false;
		m_TryTarget = pAgent->GetDebugUID();
	}

	bool DebugMgr::HasBreakPoint(const DebugTargetID& target, UINT nodeUID)
	{
		auto pGraph = FindGraphDebugInfo(target);
		if (!pGraph)
			return false;

		auto it2 = pGraph->DebugPointInfos.find(nodeUID);
		if (it2 == pGraph->DebugPointInfos.end())
			return false;
		return it2->second.HasBreakPoint();
	}

	bool DebugMgr::HasLogPoint(const DebugTargetID& target, UINT nodeUID)
	{
		auto pGraph = FindGraphDebugInfo(target);
		if (!pGraph)
			return false;

		auto it2 = pGraph->DebugPointInfos.find(nodeUID);
		if (it2 == pGraph->DebugPointInfos.end())
			return false;
		return it2->second.HasLogPoint();
	}

	bool DebugMgr::HasDebugPoint(const DebugTargetID& target, UINT nodeUID)
	{
		auto pGraph = FindGraphDebugInfo(target);
		if (!pGraph)
			return false;

		auto it2 = pGraph->DebugPointInfos.find(nodeUID);
		if (it2 == pGraph->DebugPointInfos.end())
			return false;
		return !it2->second.NoDebugPoint();
	}

	void DebugMgr::AddBreakPoint(const DebugTargetID& target, UINT nodeUID)
	{
		auto pGraph = FindGraphDebugInfo(target);
		if (!pGraph)
			return;
		DebugPointInfo info;
		info.nodeUID = nodeUID;
		info.count = 1;
		pGraph->DebugPointInfos[nodeUID] = info;
	}

	void DebugMgr::AddLogPoint(const DebugTargetID& target, UINT nodeUID)
	{
		auto pGraph = FindGraphDebugInfo(target);
		if (!pGraph)
			return;
		DebugPointInfo info;
		info.nodeUID = nodeUID;
		info.count = -1;
		pGraph->DebugPointInfos[nodeUID] = info;
	}

	void DebugMgr::AddTreeDebugInfo(const DebugTargetID& target, GraphDebugInfo&& info)
	{
		if (target.Type == DebugTargetType::TREE)
			m_TreeDebugInfo[target.Name] = info;
		else
			m_FSMDebugInfo = info;
	}

	void DebugMgr::RemoveDebugPoint(const DebugTargetID& target, UINT nodeUID)
	{
		auto pGraph = FindGraphDebugInfo(target);
		if (!pGraph)
			return;

		pGraph->DebugPointInfos.erase(nodeUID);
	}

	YBehavior::GraphDebugInfo* DebugMgr::FindGraphDebugInfo(const DebugTargetID& target)
	{
		if (target.Type == DebugTargetType::FSM)
		{
			return &m_FSMDebugInfo;
		}
		auto it = m_TreeDebugInfo.find(target.Name);
		if (it == m_TreeDebugInfo.end())
			return nullptr;
		return &(it->second);
	}

	NodeRunInfo* DebugMgr::CreateAndAppendRunInfo()
	{
		ScopedLock lock(m_Mutex);
		NodeRunInfo* pInfo = ObjectPool<NodeRunInfo>::Get();
		m_RunInfos.push_back(pInfo);
		return pInfo;
	}

	void DebugMgr::Clear()
	{
		ScopedLock lock(m_Mutex);
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
				STRING content(name + IDebugHelper::s_SequenceSpliter + iarray->GetToString(it.Value()));
				if (buffer.length() > 0)
					buffer += IDebugHelper::s_ListSpliter;
				buffer += content;
			}
		}
	}

	///> [TickResult]
	///> H
	///> MainDatas
	///> C
	///> FSM RunInfos
	///> C
	///> SubTreeName
	///> C
	///> SubTree LocalData
	///> C
	///> SubTree RunInfos

	void DebugMgr::SendInfos(AgentPtr pTarget, bool clear)
	{
		AppendSendContent("[TickResult]");
		AppendSendContent(IDebugHelper::s_HeadSpliter);

		///> SharedDatas:

		///> MainData
		{
			STRING buffer;
			SharedDataEx* pSharedData = pTarget->GetMemory()->GetMainData();

			_OutputMemoryInfo(pSharedData, buffer);
			AppendSendContent(buffer);
		}


		std::unordered_map<BehaviorTree*, std::pair<STRING, STRING>> treeBuffer;
		STRING fsmBuffer;

		///> LocalData
		{
			MemoryStack& localStack = pTarget->GetMemory()->GetStack();
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
		{
			ScopedLock lock(GetMutex());
			const std::list<NodeRunInfo*>& runInfos = GetTreeRunInfos();
			for (auto it : runInfos)
			{
				STRING* buf = nullptr;
				if (it->type == DebugTargetType::FSM)
					buf = &fsmBuffer;
				else
					buf = &(treeBuffer[(BehaviorTree*)it->pNode].second);
				if (buf->length() > 0)
					*buf += IDebugHelper::s_ListSpliter;
				*buf += it->ToString();
			}
		}
	
		AppendSendContent(IDebugHelper::s_ContentSpliter);
		AppendSendContent(fsmBuffer);
		
		for (auto it = treeBuffer.begin(); it != treeBuffer.end(); ++it)
		{
			AppendSendContent(IDebugHelper::s_ContentSpliter);
			AppendSendContent(it->first->GetTreeName());
			AppendSendContent(IDebugHelper::s_ContentSpliter);
			AppendSendContent(it->second.first);
			AppendSendContent(IDebugHelper::s_ContentSpliter);
			AppendSendContent(it->second.second);
		}

		//if (IsPaused())
		//	LOG_BEGIN << "Dont Clear Run Info cause paused" << LOG_END;
		Send(clear);
	}

	/////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////

	unsigned IDebugHelper::s_Token = 0;

	void IDebugHelper::CreateRunInfo()
	{
		m_pRunInfo = DebugMgr::Instance()->CreateAndAppendRunInfo();
	}

	void IDebugHelper::SetResult(int rawState, int finalState)
	{
		if (IsValid())
		{
			m_pRunInfo->rawRunState = rawState;
			m_pRunInfo->finalRunState = finalState;
		}
	}

	void IDebugHelper::_SendPause()
	{
		DebugMgr::Instance()->AppendSendContent("[Paused]");
		DebugMgr::Instance()->Send(false);
	}

	void IDebugHelper::TryPause()
	{
		if (!IsValid())
			return;

		///> No sub nodes have run. No need to pause
		if (m_Token == s_Token)
			return;

		if (DebugMgr::Instance()->IsPaused())
			_Breaking();
	}

	void IDebugHelper::_SetBreak()
	{
		///> Send runInfos to editor
		DebugMgr::Instance()->TogglePause(true);
		DebugMgr::Instance()->SetStepOverHelper(nullptr);
		LOG_BEGIN << "Breaking..." << LOG_END;
		SetResult(NS_BREAK, NS_BREAK);

		_Breaking();
	}

	void IDebugHelper::_Breaking()
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
		SetResult(NS_RUNNING, NS_RUNNING);
		LOG_BEGIN << "Continue." << LOG_END;
	}

	void IDebugHelper::_SendCurrentInfos()
	{
		if (!IsValid())
			return;

		DebugMgr::Instance()->SendInfos(m_Target, false);
	}

	void IDebugHelper::TryBreaking()
	{
		if (!IsValid())
			return;

		if (DebugMgr::Instance()->IsPaused())
			_Breaking();
		else if (DebugMgr::Instance()->HasBreakPoint({ m_Type, GetRootName() }, m_pRunInfo->nodeUID))
			_SetBreak();
	}

	bool IDebugHelper::HasLogPoint()
	{
		if (!IsValid())
			return false;
		return DebugMgr::Instance()->HasLogPoint({ m_Type, GetRootName() }, m_pRunInfo->nodeUID);
	}

	bool IDebugHelper::HasDebugPoint()
	{
		if (!IsValid())
			return false;
		return DebugMgr::Instance()->HasDebugPoint({ m_Type, GetRootName() }, m_pRunInfo->nodeUID);
	}
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
	DebugTreeHelper::DebugTreeHelper(Agent* pAgent, BehaviorNode* pNode)
		: IDebugHelper(pAgent)
		, m_pLogInfo(nullptr)
	{
		if (pAgent == nullptr || !DebugMgr::Instance()->IsValidTarget(pAgent, pNode->GetRoot()))
		{
			m_Target = nullptr;
			return;
		}

		m_pNode = pNode;
		m_Type = DebugTargetType::TREE;

		CreateRunInfo();
		m_pRunInfo->nodeUID = pNode->GetUID();
		m_pRunInfo->rawRunState = NS_RUNNING;
		m_pRunInfo->finalRunState = NS_RUNNING;
		m_pRunInfo->type = DebugTargetType::TREE;
		m_pRunInfo->pNode = pNode->GetRoot();

		if (DebugMgr::Instance()->HasDebugPoint({ DebugTargetType::TREE, pNode->GetRoot()->GetTreeName() }, pNode->GetUID()))
		{
			m_pLogInfo = ObjectPool<NodeLogInfo>::Get();
			m_pLogInfo->Reset();
			pNode->GetDebugLogInfo().str("");
		}

		m_Token = ++s_Token;
	}

	DebugTreeHelper::~DebugTreeHelper()
	{
		if (IsValid())
		{
			_SendLogPoint();

			if (this == DebugMgr::Instance()->GetStepOverHelper())
			{
				DebugMgr::Instance()->SetStepOverHelper(nullptr);
				DebugMgr::Instance()->TogglePause(true);
			}
		}

		if (m_pLogInfo)
			ObjectPool<NodeLogInfo>::Recycle(m_pLogInfo);
	}

	const YBehavior::STRING& DebugTreeHelper::GetRootName()
	{
		return m_pNode->GetRoot()->GetTreeName();
	}

	void DebugTreeHelper::_SendLogPoint()
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
		DebugMgr::Instance()->AppendSendContent(Utility::ToString<INT>((INT)m_pRunInfo->rawRunState));
		DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
		DebugMgr::Instance()->AppendSendContent(Utility::ToString<INT>((INT)m_pRunInfo->finalRunState));
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

	void DebugTreeHelper::LogSharedData(ISharedVariableEx* pVariable, bool bBefore)
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
			ss << sharedDataVariableName;
			if (pVariable->IsLocal())
				ss << "'";

			///>  Like:      Int0 IntArrayM[7] 44
			if (pVectorIndex)
			{

				ss << "[" << pVectorIndex->GetValueToSTRING(m_Target->GetMemory()) << "] ";
			}
			///>  Like:      Int0 IntC 4
			else
			{
				ss << " ";
			}
			ss << pVariable->GetValueToSTRING(m_Target->GetMemory());
		}

		STRING res(ss.str());
		if (bBefore)
			m_pLogInfo->beforeInfo.push_back(res);
		else
			m_pLogInfo->afterInfo.push_back(res);
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	DebugFSMHelper::DebugFSMHelper(Agent* pAgent, MachineState* pNode)
		: IDebugHelper(pAgent)
		, m_pNode(pNode)
		, m_pLogInfo(nullptr)
	{
		if (pAgent == nullptr || !DebugMgr::Instance()->IsValidTarget(pAgent, pNode->GetParentMachine()->GetRootMachine()->GetFSM()))
		{
			m_Target = nullptr;
			return;
		}

		m_pNode = pNode;
		m_Type = DebugTargetType::FSM;

		CreateRunInfo();
		m_pRunInfo->nodeUID = pNode->GetUID();
		m_pRunInfo->rawRunState = NS_RUNNING;
		m_pRunInfo->finalRunState = NS_RUNNING;
		m_pRunInfo->type = DebugTargetType::FSM;
		m_pRunInfo->pNode = pNode->GetParentMachine()->GetRootMachine()->GetFSM();

		if (DebugMgr::Instance()->HasDebugPoint({ DebugTargetType::FSM, pNode->GetParentMachine()->GetRootMachine()->GetFSM()->GetName() }, pNode->GetUID()))
		{
			m_pLogInfo = ObjectPool<NodeLogInfo>::Get();
			m_pLogInfo->Reset();
			pNode->GetDebugLogInfo().str("");
		}

		m_Token = ++s_Token;
	}

	DebugFSMHelper::~DebugFSMHelper()
	{
		if (IsValid())
		{
			_SendLogPoint();

			if (this == DebugMgr::Instance()->GetStepOverHelper())
			{
				DebugMgr::Instance()->SetStepOverHelper(nullptr);
				DebugMgr::Instance()->TogglePause(true);
			}
		}

		if (m_pLogInfo)
			ObjectPool<NodeLogInfo>::Recycle(m_pLogInfo);
	}

	const YBehavior::STRING& DebugFSMHelper::GetRootName()
	{
		return m_pNode->GetParentMachine()->GetRootMachine()->GetFSM()->GetName();
	}

	void DebugFSMHelper::_SendLogPoint()
	{
		if (m_pLogInfo == nullptr)
			return;

		m_pLogInfo->otherInfo = m_pNode->GetDebugLogInfo().str();
		m_pNode->GetDebugLogInfo().str("");

		DebugMgr::Instance()->AppendSendContent("[LogPoint]");
		DebugMgr::Instance()->AppendSendContent(s_HeadSpliter);
		DebugMgr::Instance()->AppendSendContent((m_pNode->GetParentMachine()->GetRootMachine()->GetFSM()->GetName()));
		DebugMgr::Instance()->AppendSendContent(" ");
		DebugMgr::Instance()->AppendSendContent(Utility::ToString(m_pNode->GetUID()));
		DebugMgr::Instance()->AppendSendContent(".");
		DebugMgr::Instance()->AppendSendContent(m_pNode->GetName());
		DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
		DebugMgr::Instance()->AppendSendContent(Utility::ToString<INT>((INT)m_pRunInfo->rawRunState));
		DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);
		DebugMgr::Instance()->AppendSendContent(Utility::ToString<INT>((INT)m_pRunInfo->finalRunState));
		DebugMgr::Instance()->AppendSendContent(s_ContentSpliter);

		if (m_pLogInfo->otherInfo.size() > 0)
			DebugMgr::Instance()->AppendSendContent(m_pLogInfo->otherInfo);

		DebugMgr::Instance()->Send(false);
	}


}
#endif // DEBUGGER
