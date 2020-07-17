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
			UINT Min{1};
			UINT Max{0};
			UINT Avg{};
			UINT Med{};
			float Ratio{ 0.0f };
			friend void operator<<(UINT target[], const StatisticResult &res)
			{
				target[0] = res.Med;
				target[1] = res.Avg;
				target[2] = res.Min;
				target[3] = res.Max;
			}

			inline bool IsValid() const { return Max >= Min; }
		};

		class StatisticResultHelper
		{
			std::vector<UINT> nums;
			public:
				void Push(UINT n) { nums.push_back(n); }
				UINT size() { return (UINT)nums.size(); }
				StatisticResult Calc(float ratio = 0.0f);
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
				StatisticResult selfTime;
				StatisticResult totalTime;

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
				StatisticResultHelper selfTime;
				StatisticResultHelper totalTime;
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
