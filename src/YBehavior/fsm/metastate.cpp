#include "YBehavior/fsm/metastate.h"
#include "YBehavior/logger.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	MetaState::MetaState(const STRING& name)
		: MachineState(name, MST_Meta)
		, m_pSubMachine(nullptr)
	{

	}

	MetaState::~MetaState()
	{
		if (m_pSubMachine)
			delete m_pSubMachine;
	}

	MachineRunRes MetaState::OnEnter(AgentPtr pAgent)
	{
		if (pAgent->GetMachineContext()->GetCurRunningState() == nullptr)
		{
			LOG_BEGIN << ToString() << " OnMetaEnter" << LOG_END;
			MachineState::OnEnter(pAgent);
		}

		MachineRunRes res;
		res = m_pSubMachine->OnEnter(pAgent);

		switch (res)
		{
		case YBehavior::MRR_Normal:
		case YBehavior::MRR_Running:
		case YBehavior::MRR_Break:
			return res;
		case YBehavior::MRR_Exit:
			res = OnExit(pAgent);
			if (res == MRR_Normal)
				return MRR_Exit;
			break;
		default:
			break;
		}

		return MRR_Normal;
	}


	MachineRunRes MetaState::OnExit(AgentPtr pAgent)
	{
		MachineRunRes res = m_pSubMachine->OnExit(pAgent);
		if (res != MRR_Normal)
			return res;

		MachineState::OnExit(pAgent);
		LOG_BEGIN << ToString() << " OnMetaExit" << LOG_END;
		return MRR_Normal;
	}

}
