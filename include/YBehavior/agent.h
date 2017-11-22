#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddata.h"

namespace YBehavior
{
	class YBEHAVIOR_API Agent
	{
		SharedData m_SharedData;

	public:
		inline SharedData* GetSharedData() { return &m_SharedData; }
	};
}

#endif