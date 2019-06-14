#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include "YBehavior/shareddataex.h"

#ifdef MSVC
#include <windows.h>
#include <timeapi.h>
#else
#include <unistd.h>
#endif
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/fsm/metastate.h"
#include <iostream>

using namespace YBehavior;
int main(int argc, char** argv)
{
	StateMachine* pMain = new StateMachine(1, 1, 1);
	{
		TransitionMapKey k;
		TransitionMapValue v;

		MachineState* pIdle = new MachineState("IDLE", MST_Normal, nullptr);
		k.fromState = nullptr;
		k.trans = Transition("ToIdle");
		v.toState = pIdle;
		pMain->InsertTrans(k, v);
		pMain->SetDefault(pIdle);

		MetaState* pMove = new MetaState("MOVE");
		k.fromState = nullptr;
		k.trans = Transition("ToMove");
		v.toState = pMove;
		pMain->InsertTrans(k, v);
	
		{
			StateMachine* pMoveMachine = new StateMachine(1, 2, 1);
			{
				MachineState* pWalk = new MachineState("WALK", MST_Normal, nullptr);
				k.fromState = nullptr;
				k.trans = Transition("ToWalk");
				v.toState = pWalk;
				pMoveMachine->InsertTrans(k, v);

				pMoveMachine->SetDefault(pWalk);

				MachineState* pRun = new MachineState("RUN", MST_Normal, nullptr);
				k.fromState = nullptr;
				k.trans = Transition("ToRun");
				v.toState = pRun;
				pMoveMachine->InsertTrans(k, v);
			}
			pMove->SetMachine(pMoveMachine);
		}

		MetaState* pFight = new MetaState("FIGHT");
		k.fromState = nullptr;
		k.trans = Transition("ToFight");
		v.toState = pFight;
		pMain->InsertTrans(k, v);

		{
			StateMachine* pFightMachine = new StateMachine(1, 2, 2);
			{
				MachineState* pDetect = new MachineState("DETECT", MST_Normal, nullptr);
				k.fromState = nullptr;
				k.trans = Transition("ToDetect");
				v.toState = pDetect;
				pFightMachine->InsertTrans(k, v);

				MachineState* pSkill = new MachineState("SKILL", MST_Normal, nullptr);
				k.fromState = nullptr;
				k.trans = Transition("ToSkill");
				v.toState = pSkill;
				pFightMachine->InsertTrans(k, v);

				pFightMachine->SetDefault(pDetect);

				MachineState* pCD = new MachineState("CD", MST_Normal, nullptr);
				k.fromState = pSkill;
				k.trans = Transition("ToCD");
				v.toState = pCD;
				pFightMachine->InsertTrans(k, v);
			}
			pFight->SetMachine(pFightMachine);
		}
	}

	STRING s;
	MachineContext context;

	pMain->OnEnter(context);
	while (true)
	{
		pMain->Update(0, context);

		std::cin >> s;
		context.GetTransition().Set(s);
	}

	return 0;
}
