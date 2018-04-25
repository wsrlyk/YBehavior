#ifndef _YBEHAVIOR_SELECTOR_H_
#define _YBEHAVIOR_SELECTOR_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class Selector : public CompositeNode
	{
	protected:
		virtual NodeState Update(AgentPtr pAgent);
	};
}

#endif
