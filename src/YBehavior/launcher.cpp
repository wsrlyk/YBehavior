#include "YBehavior/launcher.h"
#include "YBehavior/network/network.h"

namespace YBehavior
{
	void LaunchCore::RegisterActions() const
	{
	}
	bool Launcher::Launch(const LaunchCore& core)
	{
		core.RegisterActions();

#ifdef DEBUGGER
		if (core.StartWithDebugListeningPort())
			Network::Instance()->InitAndCreateThread(core.StartWithDebugListeningPort());
#endif
		return true;
	}
}
