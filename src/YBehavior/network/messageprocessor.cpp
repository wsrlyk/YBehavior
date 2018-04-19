#include "YBehavior/network/messageprocessor.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/debugger.h"

namespace YBehavior
{
	void DebugTreeWithAgent(const std::vector<STRING>& datas)
	{
		DebugMgr::Instance()->SetTarget(datas[1], Utility::ToType<UINT>(datas[2]));

		DebugMgr::Instance()->ClearBreakPoints();
		for (int i = 3; i < datas.size(); ++i)
		{
			UINT uid = Utility::ToType<UINT>(datas[i]);
			DebugMgr::Instance()->AddBreakPoint(uid);
		}
	}

	void Continue()
	{
		DebugMgr::Instance()->TogglePause(false);
	}

	void MessageProcessor::ProcessOne(const STRING& s)
	{
		std::vector<STRING> datas;
		Utility::SplitString(s, datas, ' ');

		if (datas[0] == "[DebugTreeWithAgent]")
		{
			DebugTreeWithAgent(datas);
		}
		else if (datas[0] == "[Continue]")
		{
			Continue();
		}
	}

	void MessageProcessor::OnNetworkClosed()
	{
		DebugMgr::Instance()->Stop();
	}

}
