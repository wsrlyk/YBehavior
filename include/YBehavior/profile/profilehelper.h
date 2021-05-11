#ifndef _YBEHAVIOR_PROFILEHELPER_H_
#define _YBEHAVIOR_PROFILEHELPER_H_

#ifdef YPROFILER

#include "YBehavior/profile/profiler.h"

namespace YBehavior
{
	class BehaviorTree;
	class TreeNode;

	namespace Profiler
	{
		class ProfileHelper
		{
		public:
			ProfileHelper();
			virtual ~ProfileHelper() {}
			void Pause();
			void Resume();
		protected:
			void _Finish();
			TimePoints m_TimePoints;
			Duration m_Duration;
			bool m_bPausing{ false };
		};

		class AgentProfileHelper : public ProfileHelper
		{
		public:
			AgentProfileHelper(AgentPtr pAgent);
			~AgentProfileHelper();
		private:
			ProfileTick* m_pTick;
		};

		class TreeProfileHelper : public ProfileHelper
		{
		public:
			TreeProfileHelper(BehaviorTree* pTree);
			~TreeProfileHelper();
		protected:
			const void* m_Key;
		};

		class TreeNodeProfileHelper : public ProfileHelper
		{
		public:
			TreeNodeProfileHelper(TreeNode* pNode);
			~TreeNodeProfileHelper();
		protected:
			const void* m_Tree;
			const void* m_Key;
		};
	}
}

#endif

#endif
