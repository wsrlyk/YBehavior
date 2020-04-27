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
			friend std::ostream & operator<<(std::ostream &ss, const StatisticResult &res)
			{
				if (res.Min == res.Max)
				{
					if (res.Ratio != 0.0f)
						_FillWithSameValue(ss, res.Min * res.Ratio);
					else
						_FillWithSameValue(ss, res.Min);
				}
				else
				{
					if (res.Ratio != 0.0f)
						_FillWithDiffValue(ss, res.Med * res.Ratio, res.Avg * res.Ratio, res.Min * res.Ratio, res.Max * res.Ratio);
					else
						_FillWithDiffValue(ss, res.Med, res.Avg, res.Min, res.Max);
				}

				return ss;
			}
			template<typename T>
			static void _FillWithSameValue(std::ostream &ss, T v)
			{
				ss << v;
			}
			template<typename T>
			static void _FillWithDiffValue(std::ostream &ss, T med, T avg, T min, T max)
			{
				ss << "Med " << med << "  Avg " << avg << "  Min " << min << "  Max " << max;
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
