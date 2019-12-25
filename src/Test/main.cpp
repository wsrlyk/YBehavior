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

int main(int argc, char** argv)
{
	MyLaunchCore core;
	YB::Launcher::Launch(core);

	XAgent::InitData();

	std::string tree("Monster_BlackCrystal3");
	if (argc >= 2)
		tree = argv[1];
	StdVector<int> a(1);
	//XEntity* pEntity = new XEntity("Hehe", tree);
	std::vector<std::string> subtest{ "Sub0", "SubTree", "Sub1", "Test" };
	XEntity* pEntity = new XEntity("Hehe", tree, &subtest);

	//XEntity* pEntity1 = new XEntity("Haha", tree, &subtest);

	//std::vector<std::string> subtest2{ "Sub0", "SubTree", "Sub1", "G", "Sub2", "Test" };
	//XEntity* pEntity2 = new XEntity("Haha", tree, &subtest2);

	//pEntity1->GetAgent()->SetEntity(pEntity1);
	LOG_FORMAT("te%dst%s abc", 1, "++");

	LOG_BEGIN << "wrapper begin" << LOG_END;
	YB::EntityWrapper wrapper = pEntity->GetAgent()->GetEntity()->GetWrapper();
	LOG_BEGIN << "{" << LOG_END;
	{
		YB::EntityWrapper wrapper0 = pEntity->GetAgent()->GetEntity()->GetWrapper();
		LOG_BEGIN << "wrapper00" << LOG_END;
		YB::EntityWrapper wrapper00 = wrapper0;
		LOG_BEGIN << "wrapper_0" << LOG_END;
		YB::EntityWrapper wrapper_0 = wrapper;
	}
	LOG_BEGIN << "}" << LOG_END;
	YB::EntityWrapper wrapper1 = pEntity->GetAgent()->GetEntity()->GetWrapper();
	LOG_BEGIN << "wrapper_1" << LOG_END;
	YB::EntityWrapper wrapper_1 = wrapper;

	YBehavior::KEY f = YBehavior::TreeKeyMgr::Instance()->GetKeyByName<YBehavior::INT>("b");
	const YB::SharedVariableCreateHelperMgr::HelperMapType& maps = YB::SharedVariableCreateHelperMgr::GetAllHelpers();
	for (auto it = maps.begin(); it != maps.end(); ++it)
	{
		if (it->second->TrySetSharedData(pEntity->GetAgent()->GetMemory()->GetMainData(), "a", "444"))
			break;
	}

	unsigned long t1 = 0, t2 = 0;

#if _MSC_VER
#pragma comment(lib, "winmm.lib ")
	t1 = (unsigned long)timeGetTime();
#else
#endif

	for (int i = 0; i < 10000; ++i)
	{
		pEntity->GetAgent()->GetMemory()->GetMainData()->Get<YBehavior::INT>(f);
	}
#if _MSC_VER
	t2 = (unsigned long)timeGetTime();
#else
#endif

	unsigned long res = t2 - t1;
	LOG_BEGIN << "cost: " << res << LOG_END;

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
