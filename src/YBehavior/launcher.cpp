#include "YBehavior/launcher.h"

namespace YBehavior
{
	void LaunchCore::RegisterActions() const
	{
	}
	bool Launcher::Launch(const LaunchCore& core)
	{
		core.RegisterActions();
		return true;
	}
}
