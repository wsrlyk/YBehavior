#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/fsm/behavior.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/agent.h"
#include "YBehavior/behaviortree.h"
#ifdef YDEBUGGER
#include "YBehavior/debugger.h"
#endif

namespace YBehavior
{
#ifdef YDEBUGGER
	//bool MachineState::_HasLogPoint()
	//{
	//	return m_pDebugHelper && m_pDebugHelper->HasDebugPoint();
	//}
#define DEBUG_RETURN(helper, rawres)\
	{\
		helper.SetResult(rawres, rawres);\
		return (rawres);\
	}
#else
#define DEBUG_RETURN(helper, res)\
	return (res)
#endif

	MachineState::MachineState(const STRING& name, MachineStateType type)
		: m_Name(name)
		, m_Type(type)
	{
		m_UID = 0;
	}

	MachineState::MachineState()
		: m_Name("")
		, m_Type(MST_Normal)
	{
		m_UID = 0;
	}

	MachineState::~MachineState()
	{
	}

	STRING MachineState::ToString() const
	{
		return m_Name + " " + Utility::ToString(m_UID);
	}

	MachineRunRes MachineState::Execute(AgentPtr pAgent, MachineRunRes previousState)
	{
#ifdef YDEBUGGER
		DebugFSMHelper dbgHelper(pAgent, this);
		m_pDebugHelper = &dbgHelper;
#endif

		///> check breakpoint
#ifdef YDEBUGGER
		dbgHelper.TryBreaking();
#endif

		auto res = _OnUpdate(pAgent);

		///> postprocessing
#ifdef YDEBUGGER
			//YB_LOG_INFO(" Return " << s_NodeStateMap.GetValue(state, Utility::StringEmpty));

		dbgHelper.TryPause();
		m_pDebugHelper = nullptr;
#endif

		DEBUG_RETURN(dbgHelper, res);
	}

	MachineRunRes MachineState::_OnUpdate(AgentPtr pAgent)
	{
		////LOG_BEGIN << ToString() << " OnUpdate" << LOG_END;

		return _RunTree(pAgent);
	}

	MachineRunRes MachineState::_RunTree(AgentPtr pAgent)
	{
		////LOG_BEGIN << "-------------------------------------" << LOG_END;

		BehaviorTree* pTree = pAgent->GetBehavior()->GetMappedTree(this);
		if (pTree)
		{
			pAgent->GetMachineContext()->SetCurRunning(pTree);

			auto treeContext = pAgent->GetTreeContext();
			NodeState lastState = NS_RUNNING;
			if (treeContext->IsCallStackEmpty())
			{
				treeContext->PushCallStack(pTree->CreateRootContext());
				lastState = NS_INVALID;
			}

			while (!treeContext->IsCallStackEmpty())
			{
				auto pContext = treeContext->GetCallStackTop();
				////LOG_BEGIN << pContext->GetTreeNode()->GetClassName() << LOG_END;
				lastState = pContext->Execute(pAgent, lastState);
				if (lastState == NS_BREAK)
				{
					break;
				}
				if (lastState == NS_RUNNING)
				{
					///> leaf node
					if (!treeContext->HasNewCall())
					{
						pContext->SendLog();
						break;
					}
				}
				else if (lastState != NS_INVALID)
				{
					treeContext->PopCallStack();
					pContext->GetTreeNode()->DestroyContext(pContext);
				}
			}

			////////NodeState ns = pTree->RootExecute(pAgent, pAgent->IsRCEmpty() ? NS_INVALID : NS_RUNNING);
			switch (lastState)
			{
			case YBehavior::NS_BREAK:
				return MRR_Break;
			case YBehavior::NS_RUNNING:
				return MRR_Running;
			default:
				break;
			}
			pAgent->GetMachineContext()->ResetCurRunning();
		}

		return MRR_Normal;

	}
}
