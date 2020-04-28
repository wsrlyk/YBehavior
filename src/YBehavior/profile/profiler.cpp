#ifdef YPROFILER
#include "YBehavior/profile/profiler.h"
#include <assert.h>
#include "YBehavior/agent.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/profile/profileprocessor.h"
#include <fstream>

namespace YBehavior
{
	namespace Profiler
	{
		void ProfileMgr::Stop()
		{
			m_bProfiling = false;

			///> Process each agent
		}

		void ProfileMgr::Clear()
		{
			m_Profiles.clear();
			m_pProfileTickCache = nullptr;
		}

		ProfileTick* ProfileMgr::NewTick(UINT64 agentUID, const STRING& name)
		{
			Profile* pProfile = &m_Profiles[agentUID];
			pProfile->agentUID = agentUID;
			pProfile->name = name;
			pProfile->profiles.push_back({});

			m_pProfileTickCache = &(*pProfile->profiles.rbegin());
			return m_pProfileTickCache;
		}

		STRING ProfileMgr::Print()
		{
			std::stringstream ss;
			std::stringstream sstemp;

			OutputRow row;
			row.time = Utility::GetDayTime();
			for (auto it = m_Profiles.begin(); it != m_Profiles.end(); ++it)
			{
				row.ClearAgent();

				ProfileProcessor processor(it->second);
				auto& res = processor.GetStatistics();
				sstemp.str("");
				sstemp << res.agentUID << "." << res.name;
				row.agent = sstemp.str();

				row.count[0] = res.tickCount;
				row.totalTime << res.time;

				_Print(row, ss);
				for (auto& tree : res.trees)
				{
					row.ClearTree();
					row.tree = tree.treeName;
					row.count << tree.runCount;
					row.totalTime << tree.totalTime;
					row.selfTime << tree.selfTime;
					_Print(row, ss);

					for (auto& node : tree.nodes)
					{
						row.ClearNode();
						sstemp.str("");
						sstemp << node.uid << '.' << node.nodeName;
						row.node = sstemp.str();
						row.count << node.runCount;
						row.totalTime << node.totalTime;
						row.selfTime << node.selfTime;
						_Print(row, ss);
					}
				}
			}
			return ss.str();
		}

		void ProfileMgr::_Print(OutputRow& row, std::stringstream& ss)
		{
			if (row.count[1] > 0)
			{
				row.totalTime[4] = row.totalTime[1] / row.count[1];
				row.selfTime[4] = row.selfTime[1] / row.count[1];
			}
			ss << row.time << '\t' << row.agent << '\t' << row.tree << '\t' << row.node
				<< '\t' << row.count[0] << '\t' << row.count[1] << '\t' << row.count[2] << '\t' << row.count[3]
				<< '\t' << row.totalTime[0] * 0.001f << '\t' << row.totalTime[1] * 0.001f << '\t' << row.totalTime[2] * 0.001f << '\t' << row.totalTime[3] * 0.001f << '\t' << row.totalTime[4] * 0.001f
				<< '\t' << row.selfTime[0] * 0.001f << '\t' << row.selfTime[1] * 0.001f << '\t' << row.selfTime[2] * 0.001f << '\t' << row.selfTime[3] * 0.001f << '\t' << row.selfTime[4] * 0.001f
				<< std::endl;
		}

		void ProfileMgr::Output(const STRING& path, const STRING& fileNamePrefix)
		{
			std::stringstream fileSS;
			fileSS << path << '/' << fileNamePrefix << '_' << Utility::GetDay() << ".txt";
			std::ofstream fs(fileSS.str(), std::ios::ate | std::ios::app);
			if (fs.tellp() == 0)
			{
				///> Column Head
				fs << "Time" << '\t' << "Agent" << '\t' << "Tree" << '\t' << "Node"
					<< '\t' << "Count(Med)" << '\t' << "Count(Avg)" << '\t' << "Count(Min)" << '\t' << "Count(Max)"
					<< '\t' << "TotalTime(Med)(ms)" << '\t' << "TotalTime(Avg)(ms)" << '\t' << "TotalTime(Min)(ms)" << '\t' << "TotalTime(Max)(ms)" << '\t' << "TotalTime(UnitAvg)(ms)"
					<< '\t' << "SelfTime(Med)(ms)" << '\t' << "SelfTime(Avg)(ms)" << '\t' << "SelfTime(Min)(ms)" << '\t' << "SelfTime(Max)(ms)" << '\t' << "SelfTime(UnitAvg)(ms)"
					<< std::endl;
			}
			fs << Print();
			fs.close();
		}

	}
}
#endif // YDEBUGGER
