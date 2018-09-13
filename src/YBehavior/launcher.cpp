#include "YBehavior/launcher.h"
#include "YBehavior/network/network.h"
#include "YBehavior/behaviortreemgr.h"

namespace YBehavior
{
	void LaunchCore::RegisterActions() const
	{
	}
	bool Launcher::Launch(const LaunchCore& core)
	{
		LogProcessDelegate* pLog = nullptr;
		ErrorProcessDelegate* pError = nullptr;
		core.GetLogProcessor(pLog, pError);
		LogMgr::Instance()->SetProcessor(pLog, pError);


		core.RegisterActions();
		TreeMgr::Instance()->SetWorkingDir(core.WorkingDir());
#ifdef DEBUGGER
		if (core.StartWithDebugListeningPort())
			Network::Instance()->InitAndCreateThread(core.StartWithDebugListeningPort());
#endif
		return true;
	}
}
