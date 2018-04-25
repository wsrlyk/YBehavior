#include "YBehavior/nodes/decorator.h"

namespace YBehavior
{
	NodeState AlwaysSuccess::Update(AgentPtr pAgent)
	{
		SingleChildNode::Update(pAgent);
		return NS_SUCCESS;
	}
	NodeState AlwaysFailed::Update(AgentPtr pAgent)
	{
		SingleChildNode::Update(pAgent);
		return NS_FAILED;
	}

	YBehavior::NodeState Invertor::Update(AgentPtr pAgent)
	{
		NodeState state = SingleChildNode::Update(pAgent);
		if (state == NS_SUCCESS)
			return NS_FAILED;
		return NS_SUCCESS;
	}

}