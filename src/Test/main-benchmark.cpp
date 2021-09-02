#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include "YBehavior/shareddataex.h"

#ifdef YB_MSVC
#include <stdio.h>
#include <windows.h>
#include <conio.h>
#else
#include <unistd.h>
#endif
#include "YBehavior/profile/profiler.h"

using namespace YBehavior;

int main(int argc, char** argv)
{
	MyLaunchCore core;
	YBehavior::Launcher::Launch(core);

	XAgent::InitData();

	XEntity* pEntity = new XEntity("Hehe", "StateMachine/BenchMarkFSM");

	char ch;
	bool notexit = true;
#ifdef YPROFILER
	Profiler::ProfileMgr::Instance()->Start();
#endif
	while (notexit)
	{
#if _MSC_VER
		if (_kbhit())
#else
#endif
		{
			ch = _getch();
			switch (ch)
			{
			case 'e':
			{
#ifdef YPROFILER
				Profiler::ProfileMgr::Instance()->Stop();
				Profiler::ProfileMgr::Instance()->Output("", "profile");
				Profiler::ProfileMgr::Instance()->Clear();
#endif
				break;
			}
			case 'b':
			{
#ifdef YPROFILER
				Profiler::ProfileMgr::Instance()->Start();
#endif
				break;
			}
			case 27:
				notexit = false;
				break;
			default:
				break;
			}
		}

#if _MSC_VER
		Sleep(300);
#else
		usleep(1000000);
#endif

		pEntity->GetAgent()->Update();
	}
	return 0;
}
