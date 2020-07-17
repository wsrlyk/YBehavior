#ifndef _YBEHAVIOR_METASTATE_H_
#define _YBEHAVIOR_METASTATE_H_

#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/fsm/statemachine.h"

namespace YBehavior
{
	class MetaState : public MachineState
	{
	protected:
		StateMachine* m_pSubMachine;

	public:
		MetaState(const STRING& name);
		~MetaState();
		inline StateMachine* GetSubMachine() { return m_pSubMachine; }
		inline void SetSubMachine(StateMachine* sm) { m_pSubMachine = sm; }
	};
}

#endif