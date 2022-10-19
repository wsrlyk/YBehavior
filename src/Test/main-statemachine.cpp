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
#include "YBehavior/types/smallmap.h"

using namespace YBehavior;
int main(int argc, char** argv)
{
	small_map<int, STRING> map{{6,"666"}};
	map.insert(0, "hehe");
	map.insert(1, "fuck");
	map[2] = "truck";
	auto res = map[3];
	if (map.find(2) != map.end())
		map[4] = "444";
	if (map.find(7) != map.end())
		map[8] = "888";

	for (auto it = map.begin(); it != map.end(); ++it)
	{
		std::cout << (*it).first() << " " << it->second() << " ";
	}
	std::cout << std::endl;
	map.erase(3);
	map.erase(map.find(2));

	const auto& m = map;
	for (auto it = m.begin(); it != m.end(); ++it)
	{
		std::cout << it->first() << " " << (*it).second() << " ";
	}
	std::cout << std::endl;

	MyLaunchCore core;
	YB::Launcher::Launch(core);

	XAgent::InitData();

	BehaviorProcessHelper::Load({ "EmptyFSM" }, { "SubTree" });

	STRING s;
	XEntity* pEntity = new XEntity("Hehe", "EmptyFSM", nullptr, nullptr);

	//std::cin >> s;

	//pMain->OnEnter(pEntity->GetAgent());
	while (true)
	{
		//pMain->Update(0, pEntity->GetAgent());
		std::cout << "tick" << std::endl;
		pEntity->GetAgent()->Tick();
		//std::cin >> s;
		//pEntity->GetAgent()->GetMachineContext()->GetTransition().Set(s);
#if _MSC_VER
		Sleep(300);
#else
		usleep(1000000);
#endif
	}

	return 0;
}
