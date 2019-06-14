#include "YBehavior/fsm/metastate.h"
#include "YBehavior/logger.h"

namespace YBehavior
{
	MetaState::MetaState(const STRING& name)
		: MachineState(name, MST_Meta, nullptr)
		, m_pMachine(nullptr)
	{

	}

	MetaState::~MetaState()
	{
		if (m_pMachine)
			delete m_pMachine;
	}

	MachineRunRes MetaState::OnEnter(MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnMetaEnter" << LOG_END;
		MachineState::OnEnter(context);
		if (m_pMachine->OnEnter(context) == MRR_Exit)
		{
			OnExit(context);
			return MRR_Exit;
		}
		return MRR_Normal;
	}


	MachineRunRes MetaState::OnExit(MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnMetaExit" << LOG_END;
		m_pMachine->OnExit(context);
		MachineState::OnExit(context);
		return MRR_Normal;
	}

}
