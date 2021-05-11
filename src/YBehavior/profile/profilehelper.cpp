#ifdef YPROFILER
#include "YBehavior/profile/profilehelper.h"
#include "YBehavior/agent.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	namespace Profiler
	{
#define IS_PROFILING if (ProfileMgr::Instance()->IsProfiling())

		ProfileHelper::ProfileHelper()
		{
			IS_PROFILING
			{
				m_TimePoints.startTime = Utility::GetTime();
				m_TimePoints.middleTime = m_TimePoints.startTime;
			}
		}

		void ProfileHelper::Pause()
		{
			IS_PROFILING
			{
				if (!m_bPausing)
				{
					m_bPausing = true;
					m_TimePoints.endTime = Utility::GetTime();
					m_Duration.durationMiliSelf += Utility::GetMicroDuration(m_TimePoints.middleTime, m_TimePoints.endTime);
				}
			}
		}

		void ProfileHelper::Resume()
		{
			IS_PROFILING
			{
				if (m_bPausing)
				{
					m_bPausing = false;
					m_TimePoints.middleTime = Utility::GetTime();
				}
			}
		}

		void ProfileHelper::_Finish()
		{
			m_TimePoints.endTime = Utility::GetTime();
			///> if not resume, ignore the time for self duration
			if (!m_bPausing)
				m_Duration.durationMiliSelf += Utility::GetMicroDuration(m_TimePoints.middleTime, m_TimePoints.endTime);
			m_Duration.durationMiliTotal += Utility::GetMicroDuration(m_TimePoints.startTime, m_TimePoints.endTime);
			//LOG_BEGIN << "self " << m_Duration.durationMiliSelf << " total " << m_Duration.durationMiliTotal << LOG_END;
		}

		AgentProfileHelper::AgentProfileHelper(AgentPtr pAgent)
		{
			IS_PROFILING
			{
				m_pTick = ProfileMgr::Instance()->NewTick(pAgent->GetUID(), pAgent->GetEntity()->ToString());
			}
		}

		AgentProfileHelper::~AgentProfileHelper()
		{
			IS_PROFILING
			{
				_Finish();
				m_pTick->duration = m_Duration;
			}
		}

		TreeProfileHelper::TreeProfileHelper(BehaviorTree* pTree)
		{
			IS_PROFILING
			{
				m_Key = pTree;
			}
		}

		TreeProfileHelper::~TreeProfileHelper()
		{
			IS_PROFILING
			{
				_Finish();
				auto tick = ProfileMgr::Instance()->GetCachedTick();
				auto tree = &(tick->trees[m_Key]);
				tree->duration += m_Duration;
				++tree->count;
			}
		}

		TreeNodeProfileHelper::TreeNodeProfileHelper(TreeNode* pNode)
		{
			IS_PROFILING
			{
				m_Tree = pNode->GetRoot();
				m_Key = pNode;
			}
		}
		TreeNodeProfileHelper::~TreeNodeProfileHelper()
		{
			IS_PROFILING
			{
				_Finish();
				auto tick = ProfileMgr::Instance()->GetCachedTick();
				auto tree = &(tick->trees[m_Tree]);
				auto& node = tree->nodes[m_Key];
				node.duration += m_Duration;
				++node.count;
			}
		}

	}
}
#endif // YDEBUGGER
