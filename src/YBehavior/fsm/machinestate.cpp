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
#include "YBehavior/runningcontext.h"

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
			//DEBUG_LOG_INFO(" Return " << s_NodeStateMap.GetValue(state, Utility::StringEmpty));

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
		BehaviorTree* pTree = pAgent->GetBehavior()->GetMappedTree(this);
		if (pTree)
		{
			pAgent->GetMachineContext()->SetCurRunning(pTree);

			auto treeContext = pAgent->GetTreeContext();
			NodeState lastState = NS_INVALID;
			if (treeContext->IsCallStackEmpty())
			{
				treeContext->PushCallStack(pTree->CreateRootContext());
				lastState = NS_RUNNING;
			}

			while (!treeContext->IsCallStackEmpty())
			{
				auto pContext = treeContext->GetCallStackTop();
				NodeState ns = pContext->Execute(pAgent, lastState);
				if (ns == NS_BREAK)
				{
					lastState = ns;
					break;
				}
				if (ns == NS_RUNNING)
				{
					if (treeContext->GetCallStackTop() == pContext)
					{
						lastState = ns;
						break;
					}
				}
				else if (ns != NS_INVALID)
				{
					treeContext->PopCallStack();
					pContext->GetTreeNode()->DestroyContext(pContext);
				}
				lastState = ns;
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
