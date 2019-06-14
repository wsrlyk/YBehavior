#ifndef _YBEHAVIOR_METASTATE_H_
#define _YBEHAVIOR_METASTATE_H_

#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/fsm/statemachine.h"

namespace YBehavior
{
	class MetaState : public MachineState
	{
	protected:
		StateMachine* m_pMachine;

	public:
		MetaState(const STRING& name);
		~MetaState();
		inline StateMachine* GetMachine() { return m_pMachine; }
		inline void SetMachine(StateMachine* sm) { m_pMachine = sm; }
		MachineRunRes OnEnter(MachineContext& context) override;
		MachineRunRes OnExit(MachineContext& context) override;
	};
}

#endif