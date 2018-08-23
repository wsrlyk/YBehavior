#include "YBehavior/nodes/decorator.h"

namespace YBehavior
{
	NodeState AlwaysSuccess::Update(AgentPtr pAgent)
	{
		NodeState ns = SingleChildNode::Update(pAgent);
		return ns == NS_RUNNING ? NS_RUNNING : NS_SUCCESS;
	}
	NodeState AlwaysFailure::Update(AgentPtr pAgent)
	{
		NodeState ns = SingleChildNode::Update(pAgent);
		return ns == NS_RUNNING ? NS_RUNNING : NS_FAILURE;
	}

	YBehavior::NodeState Invertor::Update(AgentPtr pAgent)
	{
		NodeState ns = SingleChildNode::Update(pAgent);
		if (ns == NS_RUNNING)
			return NS_RUNNING;
		if (ns == NS_SUCCESS)
			return NS_FAILURE;
		return NS_SUCCESS;
	}

}