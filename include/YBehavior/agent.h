#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddataex.h"

namespace YBehavior
{
	class BehaviorTree;
	class YBEHAVIOR_API Agent
	{
		SharedDataEx m_SharedData;
		BehaviorTree* m_Tree;
	public:
		Agent();
		inline SharedDataEx* GetSharedData() { return &m_SharedData; }
		void SetTree(const STRING& name);
		void Tick();
	};
}

#endif