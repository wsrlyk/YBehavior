#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/fsm/machinetreemapping.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/agent.h"
#include "YBehavior/behaviortree.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif

namespace YBehavior
{
#ifdef DEBUGGER
	bool MachineState::_HasLogPoint()
	{
		return m_pDebugHelper && m_pDebugHelper->HasDebugPoint();
	}
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
		, m_Identification(name)
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

	MachineRunRes MachineState::OnEnter(AgentPtr pAgent)
	{
		LOG_BEGIN << ToString() << " OnEnter" << LOG_END;
		return MRR_Normal;
	}

	MachineRunRes MachineState::OnExit(AgentPtr pAgent)
	{
		LOG_BEGIN << ToString() << " OnExit" << LOG_END;
		return MRR_Normal;
	}

	MachineRunRes MachineState::Execute(AgentPtr pAgent, MachineRunRes previousState)
	{
#ifdef DEBUGGER
		DebugFSMHelper dbgHelper(pAgent, this);
		m_pDebugHelper = &dbgHelper;
#endif

		///> check breakpoint
#ifdef DEBUGGER
		dbgHelper.TryBreaking();
#endif

		auto res = _OnUpdate(pAgent);

		///> postprocessing
#ifdef DEBUGGER
			//DEBUG_LOG_INFO(" Return " << s_NodeStateMap.GetValue(state, Utility::StringEmpty));

		dbgHelper.TryPause();
		m_pDebugHelper = nullptr;
#endif

		DEBUG_RETURN(dbgHelper, res);
	}

	MachineRunRes MachineState::_OnUpdate(AgentPtr pAgent)
	{
		LOG_BEGIN << ToString() << " OnUpdate" << LOG_END;

		return _RunTree(pAgent);
	}

	MachineRunRes MachineState::_RunTree(AgentPtr pAgent)
	{
		BehaviorTree* pTree = pAgent->GetMachineContext()->GetMapping()->GetTree(m_UID);
		if (pTree)
		{
			pAgent->GetMachineContext()->SetCurRunning(pTree);
			NodeState ns = pTree->RootExecute(pAgent, pAgent->IsRCEmpty() ? NS_INVALID : NS_RUNNING);
			switch (ns)
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
