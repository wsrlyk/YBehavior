#ifndef _YBEHAVIOR_DECORATOR_H_
#define _YBEHAVIOR_DECORATOR_H_
#endif

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class YBEHAVIOR_API AlwaysSuccess : public SingleChildNode
	{
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
	class YBEHAVIOR_API AlwaysFailed : public SingleChildNode
	{
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
}
