#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include <windows.h>
#include "YBehavior/shareddataex.h"
using namespace YBehavior;

int main(int argc, char* argv)
{
	MyLaunchCore core;
	YBehavior::Launcher::Launch(core);
	std::vector<int> a(1);
	XEntity* pEntity = new XEntity("Hehe");
	pEntity->GetAgent()->SetEntity(pEntity);
	//pEntity->GetAgent()->GetSharedData()->GetBool(3);

	//aa.GetValue(pEntity->GetAgent()->GetSharedData());
	//TreeMgr::Instance()->LoadOneTree("Monster_BlackCrystal");
	int i = 0;
	while(i < 100)
	{
		pEntity->GetAgent()->Tick();
		Sleep(100);
	}
	return 0;
}