#ifndef _YBEHAVIOR_SEQUENCE_H_
#define _YBEHAVIOR_SEQUENCE_H_
#endif

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class Sequence : public CompositeNode
	{
	public:
		Sequence();
		~Sequence();
	protected:
		virtual NodeState Update(AgentPtr pAgent);
	};
}
