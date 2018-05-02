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
	XEntity* arrays[3];
	for (int i = 0; i < 3; ++i)
	{
		arrays[i] = nullptr;
	}

	while (1)
	{
		char c;
		std::cin >> c;
		switch (c)
		{
		case ' ':
			return 0;
		case '0':
		case '1':
		case '2':
		{
			int index = '2' - c;
			if (arrays[index] == nullptr)
				arrays[index] = new XEntity("Hehe");
			else
			{
				delete arrays[index];
				arrays[index] = nullptr;
			}

			std::cout << (arrays[0] != nullptr) << ' ' << (arrays[1] != nullptr) << ' ' << (arrays[2] != nullptr) << ' ' << std::endl;
			TreeMgr::Instance()->Print();
			break;
		}
		case 'r':
			TreeMgr::Instance()->ReloadTree("Monster_BlackCrystal");
			TreeMgr::Instance()->Print();
			break;
		default:
			break;
		}
	}
	return 0;
}
