#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddata.h"

namespace YBehavior
{
	class BehaviorTree;
	class YBEHAVIOR_API Agent
	{
		SharedData m_SharedData;
		BehaviorTree* m_Tree;
	public:
		Agent();
		inline SharedData* GetSharedData() { return &m_SharedData; }
		void SetTree(const STRING& name);
		void Tick();
	};
}

#endif