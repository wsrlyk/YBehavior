#include "YBehavior/nodes/decorator.h"

namespace YBehavior
{
	NodeState AlwaysSuccess::Update(AgentPtr pAgent)
	{
		SingleChildNode::Update(pAgent);
		return NS_SUCCESS;
	}
	NodeState AlwaysFailure::Update(AgentPtr pAgent)
	{
		SingleChildNode::Update(pAgent);
		return NS_FAILURE;
	}

	YBehavior::NodeState Invertor::Update(AgentPtr pAgent)
	{
		NodeState state = SingleChildNode::Update(pAgent);
		if (state == NS_SUCCESS)
			return NS_FAILURE;
		return NS_SUCCESS;
	}

}