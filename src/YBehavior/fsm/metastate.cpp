#include "YBehavior/fsm/metastate.h"
#include "YBehavior/fsm/statemachine.h"

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
}
