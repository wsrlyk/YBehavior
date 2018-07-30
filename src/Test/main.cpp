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

	std::string tree("Monster_BlackCrystal3");
	if (argc >= 2)
		tree = argv[1];
	std::vector<int> a(1);
	XEntity* pEntity = new XEntity("Hehe", tree);
	pEntity->GetAgent()->SetEntity(pEntity);

	XEntity* pEntity1 = new XEntity("Haha", tree);
	pEntity1->GetAgent()->SetEntity(pEntity1);

	LOG_BEGIN << "wrapper begin" << LOG_END;
	EntityWrapper wrapper = pEntity->GetAgent()->GetEntity()->CreateWrapper();
	LOG_BEGIN << "{" << LOG_END;
	{
		EntityWrapper wrapper0 = pEntity->GetAgent()->GetEntity()->CreateWrapper();
		LOG_BEGIN << "wrapper00" << LOG_END;
		EntityWrapper wrapper00 = wrapper0;
		LOG_BEGIN << "wrapper_0" << LOG_END;
		EntityWrapper wrapper_0 = wrapper;
	}
	LOG_BEGIN << "}" << LOG_END;
	EntityWrapper wrapper1 = pEntity->GetAgent()->GetEntity()->CreateWrapper();
	LOG_BEGIN << "wrapper_1" << LOG_END;
	EntityWrapper wrapper_1 = wrapper;

	//LOG_BEGIN << "delete entity" << LOG_END;
	//delete pEntity;
	//return 0;
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
