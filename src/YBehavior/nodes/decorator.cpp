#include "YBehavior/nodes/decorator.h"
#include "YBehavior/profile/profileheader.h"

namespace YBehavior
{
	YBehavior::NodeState AlwaysSuccessNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		NodeState ns = SingleChildNodeContext::_Update(pAgent, lastState);
		if (ns == NS_RUNNING || ns == NS_BREAK)
			return ns;
		return NS_SUCCESS;
	}

	YBehavior::NodeState AlwaysFailureNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		NodeState ns = SingleChildNodeContext::_Update(pAgent, lastState);
		if (ns == NS_RUNNING || ns == NS_BREAK)
			return ns;
		return NS_FAILURE;
	}
}