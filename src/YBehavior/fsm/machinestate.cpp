#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/fsm/machinetreemapping.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/agent.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	MachineState::MachineState(const STRING& name, MachineStateType type)
		: m_Name(name)
		, m_Type(type)
		, m_Identification(name)
	{
		m_UID.Value = 0;
	}

	MachineState::MachineState()
		: m_Name("")
		, m_Type(MST_Normal)
	{
		m_UID.Value = 0;
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

	MachineRunRes MachineState::OnUpdate(float fDeltaT, AgentPtr pAgent)
	{
		LOG_BEGIN << ToString() << " OnUpdate" << LOG_END;

		return _RunTree(pAgent);
	}

	MachineRunRes MachineState::_RunTree(AgentPtr pAgent)
	{
		BehaviorTree* pTree = pAgent->GetMachineContext()->GetMapping()->GetTree(this->m_UID.Value);
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
