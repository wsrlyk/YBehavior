#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include "YBehavior/shareddataex.h"

#ifdef YB_MSVC
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

	BehaviorProcessHelper::Load({ "EmptyFSM" }, { "SubTree" });

	STRING s;
	XEntity* pEntity = new XEntity("Hehe", "EmptyFSM", nullptr, nullptr);

	//pMain->OnEnter(pEntity->GetAgent());
	while (true)
	{
		//pMain->Update(0, pEntity->GetAgent());
		pEntity->GetAgent()->Tick();
		//std::cin >> s;
		//pEntity->GetAgent()->GetMachineContext()->GetTransition().Set(s);
#if _MSC_VER
		Sleep(2000);
#else
		usleep(1000000);
#endif
	}

	return 0;
}
