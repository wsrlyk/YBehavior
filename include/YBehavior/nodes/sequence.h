#ifndef _YBEHAVIOR_SEQUENCE_H_
#define _YBEHAVIOR_SEQUENCE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class Sequence : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "Sequence"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
	};
}

#endif
