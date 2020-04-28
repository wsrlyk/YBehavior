#ifndef _YBEHAVIOR_PROFILER_H_
#define _YBEHAVIOR_PROFILER_H_

#ifdef YPROFILER

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include "YBehavior/singleton.h"
#include <unordered_map>
#include "YBehavior/utility.h"

namespace YBehavior
{
	namespace Profiler
	{
		struct TimePoints
		{
			Utility::TimePointType startTime;
			Utility::TimePointType middleTime;
			Utility::TimePointType endTime;
		};
		struct Duration
		{
			UINT durationMiliSelf{};
			UINT durationMiliTotal{};

			Duration operator+(const Duration& other)
			{
				Duration res;
				res.durationMiliSelf = this->durationMiliSelf + other.durationMiliSelf;
				res.durationMiliTotal = this->durationMiliTotal + other.durationMiliTotal;
				return res;
			}

			Duration& operator+=(const Duration& other)
			{
				this->durationMiliSelf += other.durationMiliSelf;
				this->durationMiliTotal += other.durationMiliTotal;
				return *this;
			}
		};

		struct ProfileTreeNode
		{
			Duration duration;
			UINT count{};
		};

		struct ProfileTree
		{
			Duration duration;
			UINT count{};
			std::unordered_map<const void*, ProfileTreeNode> nodes;
		};

		struct ProfileTick
		{
			Duration duration;
			std::unordered_map<const void*, ProfileTree> trees;
		};

		struct Profile
		{
			UINT64 agentUID;
			STRING name;
			std::vector<ProfileTick> profiles;
		};
		class ProfileMgr : public Singleton<ProfileMgr>
		{
			struct OutputRow
			{
				STRING time;
				STRING agent;
				STRING tree;
				STRING node;

				UINT count[4]{ 0 };
				UINT totalTime[5]{ 0 };
				UINT selfTime[5]{ 0 };

				void ClearNumbers()
				{
					memset(count, 0, sizeof(UINT) * 4);
					memset(totalTime, 0, sizeof(UINT) * 5);
					memset(selfTime, 0, sizeof(UINT) * 5);
				}
				void ClearAgent()
				{
					agent = tree = node = "";
					ClearNumbers();
				}
				void ClearTree()
				{
					tree = node = "";
					ClearNumbers();
				}
				void ClearNode()
				{
					node = "";
					ClearNumbers();
				}
			};
		protected:
			std::unordered_map<UINT64, Profile> m_Profiles;
			bool m_bProfiling{ false };
			ProfileTick* m_pProfileTickCache;
			void _Print(OutputRow& row, std::stringstream& ss);
		public:
			void Start() { m_bProfiling = true; }
			void Stop();
			void Clear();
			STRING Print();
			void Output(const STRING& path, const STRING& fileNamePrefix);
			inline bool IsProfiling() const { return m_bProfiling; }
			ProfileTick* NewTick(UINT64 agentUID, const STRING& name);
			inline ProfileTick* GetCachedTick() { return m_pProfileTickCache; }
			inline std::unordered_map<UINT64, Profile>& GetProfiles() { return m_Profiles; }

		};
	}
}

#endif

#endif
