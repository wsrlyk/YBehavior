#include "YBehavior/launcher.h"
#include "YBehavior/network/network.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/mgrs.h"

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
#ifndef YSHARP
		Mgrs::Instance()->GetTreeMgr()->SetWorkingDir(core.WorkingDir());
		Mgrs::Instance()->GetMachineMgr()->SetWorkingDir(core.WorkingDir());
#endif
#ifdef YDEBUGGER
		if (core.StartWithDebugListeningPort())
			Network::Instance()->InitAndCreateThread(core.StartWithDebugListeningPort());
#endif
		return true;
	}
}
