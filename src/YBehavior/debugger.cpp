#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#include "YBehavior/agent.h"
#include "YBehavior/tools/objectpool.h"
#include "YBehavior/network/network.h"
#include "YBehavior/logger.h"
#include "YBehavior/interface.h"
#include "YBehavior/nodefactory.h"
#include <sstream>

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

	bool DebugMgr::IsValidTarget(Agent* pAgent)
	{
		if (pAgent == nullptr)
			return false;

		if (m_bTargetDirty)
		{
			if (m_TargetAgent == 0 || m_TargetAgent == pAgent->GetUID())
			{
				if (pAgent->GetTree()->GetName() == m_TargetTree)
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

	bool DebugMgr::TryHitBreakPoint(UINT nodeUID)
	{
		auto it = m_BreakPointInfos.find(nodeUID);
		return it != m_BreakPointInfos.end();
	}

	void DebugMgr::AddBreakPoint(UINT nodeUID)
	{
		BreakPointInfo info;
		info.nodeUID = nodeUID;

		m_BreakPointInfos[nodeUID] = info;
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
	}

	DebugHelper::~DebugHelper()
	{
		if (!IsValid())
			return;

		///> Root node, send shared variables.
		if (m_pNode->GetParent() == nullptr)
		{
			BehaviorTree* tree = dynamic_cast<BehaviorTree*>(m_pNode);
			if (tree)
				_SendInfos(tree->GetName());
		}
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
				_SendInfos(tree->GetName());
		}

	}

	void DebugHelper::_SendInfos(const STRING& treeName)
	{
		DebugMgr::Instance()->AppendSendContent("[TickResult] ");

		///> SharedDatas:
		STRING buffer;
		SharedDataEx* pSharedData = m_Target->GetSharedData();

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

		DebugMgr::Instance()->AppendSendContent(" ");

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
		if (DebugMgr::Instance()->TryHitBreakPoint(m_pRunInfo->nodeUID))
			Breaking();
	}

	void DebugHelper::Breaking()
	{
		LOG_BEGIN << "Breaking..." << LOG_END;
		m_pRunInfo->runState = NS_BREAK;

		///> Send runInfos to editor
		DebugMgr::Instance()->TogglePause(true);

		SetResult(NS_BREAK);
		_SendCurrentInfos();
		///> Sleep and wait for removing break
		int i = 0;
		while (DebugMgr::Instance()->IsPaused())
		{
			Thread::SleepMilli(100);
			if (++i > 10)
				DebugMgr::Instance()->TogglePause(false);
		}

		LOG_BEGIN << "Continue." << LOG_END;
	}
}
#endif