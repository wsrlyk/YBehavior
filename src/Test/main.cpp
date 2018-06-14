#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include "YBehavior/shareddataex.h"

#ifdef MSVC
#include <windows.h>
#else
#include <unistd.h>
#endif

using namespace YBehavior;

int main(int argc, char** argv)
{
	MyLaunchCore core;
	YBehavior::Launcher::Launch(core);

	XAgent::InitData();

	std::vector<int> a(1);
	XEntity* pEntity = new XEntity("Hehe");
	pEntity->GetAgent()->SetEntity(pEntity);

	XEntity* pEntity1 = new XEntity("Haha");
	pEntity1->GetAgent()->SetEntity(pEntity1);
	//pEntity->GetAgent()->GetSharedData()->GetBool(3);

	//aa.GetValue(pEntity->GetAgent()->GetSharedData());
	//TreeMgr::Instance()->LoadOneTree("Monster_BlackCrystal");
	int i = 0;
	while(i < 100)
	{
		pEntity->GetAgent()->Update();
#if _MSC_VER
		Sleep(300);
#else
		usleep(1000000);
#endif
	}
	return 0;
}
