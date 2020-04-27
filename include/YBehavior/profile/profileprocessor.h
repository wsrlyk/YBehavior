#ifndef _YBEHAVIOR_PROFILEPROCESSOR_H_
#define _YBEHAVIOR_PROFILEPROCESSOR_H_

#ifdef YPROFILER

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include "YBehavior/utility.h"

namespace YBehavior
{
	namespace Profiler
	{
		struct Profile;
		struct ProfileTick;
		struct StatisticResult
		{
			UINT Min{};
			UINT Max{};
			UINT Avg{};
			UINT Med{};
			friend std::ostream & operator<<(std::ostream &ss, const StatisticResult &res)
			{
				ss << "Med " << res.Med << "; Avg " << res.Avg << "; Min " << res.Min << "; Max " << res.Max;
				return ss;
			}
		};

		class StatisticResultHelper
		{
			std::vector<UINT> nums;
			public:
				void Push(UINT n) { nums.push_back(n); }
				UINT size() { return nums.size(); }
				StatisticResult Calc();
		};

		struct ProfileStatistic
		{
			UINT64 agentUID;
			STRING name;

			StatisticResult time;
			UINT tickCount;

			struct TreeNodeStatistic
			{
				STRING nodeName;
				UINT uid;

				StatisticResult runCount;
				StatisticResult selfTime;
				StatisticResult totalTime;
			};
			struct TreeStatistic
			{
				STRING treeName;

				StatisticResult runCount;
				StatisticResult time;

				std::vector<TreeNodeStatistic> nodes;
			};

			std::vector<TreeStatistic> trees;
		};

		class ProfileProcessor
		{
			UINT64 agentUID;
			STRING name;

			UINT tickCount;
			StatisticResultHelper time;

			struct TreeNodeData
			{
				STRING nodeName;
				UINT uid;

				StatisticResultHelper selfTime;
				StatisticResultHelper totalTime;
				StatisticResultHelper runCount;
			};

			struct TreeData
			{
				STRING treeName;
				StatisticResultHelper time;
				StatisticResultHelper runCount;
				std::unordered_map<const void*, TreeNodeData> nodes;
			};
			std::unordered_map<STRING, TreeData> trees;

			ProfileStatistic m_Statistic;
		public:
			ProfileProcessor(const Profile& data);
			inline ProfileStatistic& GetStatistics() { return m_Statistic; }
		private:
			void _ProcessTick(const ProfileTick& tick);
			void _ProcessResult();
		};
	}
}

#endif

#endif
