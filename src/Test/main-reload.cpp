#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include "YBehavior/shareddataex.h"

#ifdef MSVC
#include <windows.h>
#else
#include <unistd.h>
#endif
#include <iostream>
#include "YBehavior/mgrs.h"
#include "YBehavior/fsm/machinemgr.h"

using namespace YBehavior;

int main(int argc, char** argv)
{
	MyLaunchCore core;
	YBehavior::Launcher::Launch(core);
	//XEntity* pEntity = new XEntity("Hehe");
	//pEntity->GetAgent()->SetEntity(pEntity);
	//pEntity->GetAgent()->GetSharedData()->GetBool(3);

	//aa.GetValue(pEntity->GetAgent()->GetSharedData());
	//TreeMgr::Instance()->LoadOneTree("Monster_BlackCrystal");
#define TOTAL 7
	XEntity* arrays[TOTAL];
	for (int i = 0; i < TOTAL; ++i)
	{
		arrays[i] = nullptr;
	}

	std::string treeName[TOTAL]{ "A", "B", "C", "D", "E", "F", "G" };
	std::unordered_set<std::string> entitycmd{ "1", "2" , "3" , "4" , "5" , "6" , "7" };
	while (1)
	{
		std::string s;
		std::cin >> s;
		if (entitycmd.count(s))
		{
			int index = s[0] - '1';
			if (arrays[index] == nullptr)
				arrays[index] = new XEntity("Hehe", "StateMachine/SimpleFSM");
			else
			{
				delete arrays[index];
				arrays[index] = nullptr;
			}

			for (int i = 0; i < TOTAL; ++i)
				std::cout << (arrays[i] != nullptr) << ' ';
			std::cout << std::endl;
			Mgrs::Instance()->GetMachineMgr()->Print();
			Mgrs::Instance()->GetTreeMgr()->Print();
		}
		else if (s == "rm")
		{
			std::cin >> s;
			Mgrs::Instance()->GetMachineMgr()->ReloadMachine(s);
			Mgrs::Instance()->GetMachineMgr()->Print();
		}
		else if (s == "rt")
		{
			std::cin >> s;
			Mgrs::Instance()->GetTreeMgr()->ReloadTree(s);
			Mgrs::Instance()->GetTreeMgr()->Print();
		}
		else if (s == "gc")
		{
			Mgrs::Instance()->GetTreeMgr()->GarbageCollection();
			Mgrs::Instance()->GetTreeMgr()->Print();
		}

		std::cout << std::endl << std::endl;
	}
	return 0;
}
