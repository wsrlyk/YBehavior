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
#define TOTAL 7
	XEntity* arrays[TOTAL];
	for (int i = 0; i < TOTAL; ++i)
	{
		arrays[i] = nullptr;
	}

	std::string treeName[TOTAL]{ "A", "B", "C", "D", "E", "F", "G" };
	while (1)
	{
		char c;
		std::cin >> c;
		switch (c)
		{
		case ' ':
			return 0;
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		{
			int index = c - '1';
			if (arrays[index] == nullptr)
				arrays[index] = new XEntity("Hehe", treeName[index]);
			else
			{
				delete arrays[index];
				arrays[index] = nullptr;
			}

			for (int i = 0; i < TOTAL; ++i)
				std::cout << (arrays[i] != nullptr) << ' ';
			std::cout << std::endl;
			TreeMgr::Instance()->Print();
			break;
		}
		case 'r':
			TreeMgr::Instance()->ReloadTree("A");
			TreeMgr::Instance()->Print();
			break;
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
		{
			std::string s(1, c + ('A' - 'a'));
			TreeMgr::Instance()->ReloadTree(s);
			TreeMgr::Instance()->Print();
			break;
		}
		case '/':
		{
			TreeMgr::Instance()->GarbageCollection();
			TreeMgr::Instance()->Print();
		}
		break;
		default:
			break;
		}
	}
	return 0;
}
