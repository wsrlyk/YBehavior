#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/launcher.h"
#include "customactions.h"
#include "YBehavior/shareddataex.h"

#ifdef MSVC
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

	auto beginTime = Utility::GetTime();
#if _MSC_VER
	Sleep(300);
#else
	usleep(1000000);
#endif
	auto endTime = Utility::GetTime();
	auto dur = Utility::GetMicroDuration(beginTime, endTime);
	LOG_BEGIN << "duration " << dur << LOG_END;

	XAgent::InitData();

	XEntity* pEntity = new XEntity("Hehe", "StateMachine/BenchMarkFSM");

	char ch;
	bool notexit = true;
	Profiler::ProfileMgr::Instance()->Start();

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
				Profiler::ProfileMgr::Instance()->Stop();
				Profiler::ProfileMgr::Instance()->Output("", "profile");
				Profiler::ProfileMgr::Instance()->Clear();
				break;
			}
			case 'b':
			{
				Profiler::ProfileMgr::Instance()->Start();
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
		auto test = Profiler::ProfileMgr::Instance();
		pEntity->GetAgent()->Update();
	}
	return 0;
}
