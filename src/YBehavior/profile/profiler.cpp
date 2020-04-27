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
			ss << "==================VVV======BEGIN======VVV================" << std::endl;
			for (auto it = m_Profiles.begin(); it != m_Profiles.end(); ++it)
			{
				ProfileProcessor processor(it->second);
				auto& res = processor.GetStatistics();

				ss << res.agentUID << "." << res.name << std::endl;
				ss << "TickCount " << res.tickCount << std::endl;
				ss << "Time" << std::endl;
				ss << '\t' << res.time << std::endl;
				ss << std::endl;

				for (auto& tree : res.trees)
				{
					ss << '\t' << tree.treeName << std::endl;
					ss << '\t' << "TickCount " << std::endl;
					ss << "\t\t" << tree.runCount << std::endl;
					ss << '\t' << "Time " << std::endl;
					ss << "\t\t" << tree.time << std::endl;
					ss << std::endl;

					for (auto& node : tree.nodes)
					{
						ss << "\t\t" << node.uid << '.' << node.nodeName << std::endl;
						ss << "\t\t" << "TickCount " << std::endl;
						ss << "\t\t\t" << node.runCount << std::endl;
						ss << "\t\t" << "Total Time " << std::endl;
						ss << "\t\t\t" << node.totalTime << std::endl;
						ss << "\t\t" << "Self Time " << std::endl;
						ss << "\t\t\t" << node.selfTime << std::endl;
						ss << std::endl;
					}
				}
			}
			ss << "==================^^^======END======^^^================" << std::endl;
			return ss.str();
		}

		void ProfileMgr::Output(const STRING& path, const STRING& fileNamePrefix)
		{
			std::stringstream fileSS;
			fileSS << path << '/' << fileNamePrefix << '_' << Utility::GetDay() << ".aip";
			std::ofstream fs(fileSS.str(), std::ios::app);

			fs << std::endl << Utility::GetDayTime() << std::endl << std::endl;
			fs << Print();
			fs.close();
		}

	}
}
#endif // YDEBUGGER
