#ifndef _YBEHAVIOR_SEQUENCE_H_
#define _YBEHAVIOR_SEQUENCE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class Sequence : public CompositeNode
	{
	protected:
		virtual NodeState Update(AgentPtr pAgent);
	};
}

#endif
