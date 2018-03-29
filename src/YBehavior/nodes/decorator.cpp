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
}