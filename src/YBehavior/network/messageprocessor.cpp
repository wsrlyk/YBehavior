#ifdef DEBUGGER
#include "YBehavior/network/messageprocessor.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/debugger.h"

namespace YBehavior
{
	void DebugTreeWithAgent(const std::vector<STRING>& datas)
	{
		DebugMgr::Instance()->SetTarget(datas[1], Utility::ToType<UINT>(datas[2]));

		std::vector<STRING> treedata;
		DebugMgr::Instance()->ClearTreeDebugInfo();
		for (unsigned i = 3; i + 1 < datas.size(); ++i)
		{
			treedata.clear();
			Utility::SplitString(datas[i], treedata, DebugHelper::s_ContentSpliter);
			TreeDebugInfo info;
			STRING name = treedata[0];
			info.Hash = Utility::ToType<UINT>(treedata[1]);

			for (unsigned j = 2; j + 1 < treedata.size(); ++j)
			{
				UINT uid = Utility::ToType<UINT>(treedata[j]);
				INT count = Utility::ToType<INT>(treedata[++j]);
				DebugPointInfo dbginfo;
				dbginfo.nodeUID = uid;
				dbginfo.count = count;

				info.DebugPointInfos[uid] = dbginfo;
			}
			
			DebugMgr::Instance()->AddTreeDebugInfo(std::move(name), std::move(info));
		}
	}

	void Continue()
	{
		DebugMgr::Instance()->TogglePause(false);
	}

	void ProcessDebugPoint(const std::vector<STRING>& datas)
	{
		const STRING& treeName = datas[1];
		UINT uid = Utility::ToType<UINT>(datas[2]);
		INT count = Utility::ToType<INT>(datas[3]);

		if (count > 0)
			DebugMgr::Instance()->AddBreakPoint(treeName, uid);
		else if (count < 0)
			DebugMgr::Instance()->AddLogPoint(treeName, uid);
		else
			DebugMgr::Instance()->RemoveDebugPoint(treeName, uid);
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
		else if (datas[0] == "[DebugPoint]")
		{
			ProcessDebugPoint(datas);
		}
	}

	void MessageProcessor::OnNetworkClosed()
	{
		DebugMgr::Instance()->Stop();
	}

}
#endif // DEBUGGER
