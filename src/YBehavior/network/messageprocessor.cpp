#ifdef YDEBUGGER
#include "messageprocessor.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/debugger.h"

namespace YBehavior
{
	void DebugAgent(const StdVector<STRING>& datas)
	{
		DebugMgr::Instance()->SetTarget(Utility::ToType<UINT64>(datas[1]), Utility::ToType<int>(datas[2]) != 0);

	}
	void DebugTree(const StdVector<STRING>& datas)
	{
		DebugMgr::Instance()->SetTarget({ DebugTargetType::TREE, datas[1] }, Utility::ToType<int>(datas[2]) != 0);

	}
	void DebugFSM(const StdVector<STRING>& datas)
	{
		DebugMgr::Instance()->SetTarget({ DebugTargetType::FSM, datas[1] }, Utility::ToType<int>(datas[2]) != 0);

	}

	void DebugBegin(const StdVector<STRING>& datas)
	{
		DebugMgr::Instance()->ClearDebugInfos();
		StdVector<STRING> treedata;
		for (unsigned i = 1; i < datas.size(); ++i)
		{
			treedata.clear();
			Utility::SplitString(datas[i], treedata, IDebugHelper::s_SequenceSpliter);
			GraphDebugInfo info;
			STRING name = treedata[0];
			//info.Hash = Utility::ToType<UINT>(treedata[1]);

			for (unsigned j = 1; j + 1 < treedata.size(); ++j)
			{
				UINT uid = Utility::ToType<UINT>(treedata[j]);
				INT count = Utility::ToType<INT>(treedata[++j]);
				DebugPointInfo dbginfo;
				dbginfo.nodeUID = uid;
				dbginfo.count = count;

				info.DebugPointInfos[uid] = dbginfo;
			}

			DebugMgr::Instance()->AddTreeDebugInfo({ i == 1 ? DebugTargetType::FSM : DebugTargetType::TREE, name }, std::move(info));
		}
		DebugMgr::Instance()->Begin();
	}
	void DebugFailed()
	{
		DebugMgr::Instance()->Failed();
	}
	void Continue()
	{
		DebugMgr::Instance()->SetCommand(DC_Continue);
	}

	void StepOver()
	{
		DebugMgr::Instance()->SetCommand(DC_StepOver);
	}

	void StepInto()
	{
		DebugMgr::Instance()->SetCommand(DC_StepInto);
	}

	void ProcessDebugPoint(DebugTargetType type, const StdVector<STRING>& datas)
	{
		const STRING& treeName = datas[1];
		UINT uid = Utility::ToType<UINT>(datas[2]);
		INT count = Utility::ToType<INT>(datas[3]);

		if (count > 0)
			DebugMgr::Instance()->AddBreakPoint({ type, treeName }, uid);
		else if (count < 0)
			DebugMgr::Instance()->AddLogPoint({ type, treeName }, uid);
		else
			DebugMgr::Instance()->RemoveDebugPoint({ type, treeName }, uid);
	}

	void MessageProcessor::ProcessOne(const STRING& s)
	{
		StdVector<STRING> datas;
		Utility::SplitString(s, datas, IDebugHelper::s_ContentSpliter, false);

		if (datas[0] == "[DebugAgent]")
		{
			DebugAgent(datas);
		}
		if (datas[0] == "[DebugTree]")
		{
			DebugTree(datas);
		}
		if (datas[0] == "[DebugFSM]")
		{
			DebugFSM(datas);
		}
		else if (datas[0] == "[Continue]")
		{
			Continue();
		}
		else if (datas[0] == "[StepInto]")
		{
			StepInto();
		}
		else if (datas[0] == "[StepOver]")
		{
			StepOver();
		}
		else if (datas[0] == "[DebugTreePoint]")
		{
			ProcessDebugPoint(DebugTargetType::TREE, datas);
		}
		else if (datas[0] == "[DebugFSMPoint]")
		{
			ProcessDebugPoint(DebugTargetType::FSM, datas);
		}
		else if (datas[0] == "[DebugBegin]")
		{
			DebugBegin(datas);
		}
		else if (datas[0] == "[DebugFailed]")
		{
			DebugFailed();
		}
	}

	void MessageProcessor::OnNetworkClosed()
	{
		DebugMgr::Instance()->Stop();
	}

}
#endif // YDEBUGGER
