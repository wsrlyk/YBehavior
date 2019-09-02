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
#include "YBehavior/fsm/transition.h"
#include "YBehavior/fsm/context.h"

using namespace YBehavior;
int main(int argc, char** argv)
{
	MyLaunchCore core;
	YB::Launcher::Launch(core);

	XAgent::InitData();

	STRING s;
	XEntity* pEntity = new XEntity("Hehe", "StateMachine/MonsterMachine", nullptr, nullptr);

	//pMain->OnEnter(pEntity->GetAgent());
	while (true)
	{
		//pMain->Update(0, pEntity->GetAgent());
		pEntity->GetAgent()->Tick();
		std::cin >> s;
		pEntity->GetAgent()->GetMachineContext()->GetTransition().Set(s);
	}

	return 0;
}
