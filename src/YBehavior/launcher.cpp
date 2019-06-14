#include "YBehavior/launcher.h"
#include "YBehavior/network/network.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/fsm/machinemgr.h"

namespace YBehavior
{
	void LaunchCore::RegisterActions() const
	{
	}
	bool Launcher::Launch(const LaunchCore& core)
	{
		LogProcessDelegate pLog = nullptr;
		ErrorProcessDelegate pError = nullptr;
		core.GetLogProcessor(pLog, pError);
		LogMgr::Instance()->SetProcessor(pLog, pError);

		pLog = nullptr;
		pError = nullptr;
		core.GetThreadLogProcessor(pLog, pError);
		LogMgr::Instance()->SetProcessorThread(pLog, pError);


		core.RegisterActions();
		TreeMgr::Instance()->SetWorkingDir(core.WorkingDir());
		MachineMgr::Instance()->SetWorkingDir(core.WorkingDir());
#ifdef DEBUGGER
		if (core.StartWithDebugListeningPort())
			Network::Instance()->InitAndCreateThread(core.StartWithDebugListeningPort());
#endif
		return true;
	}
}
