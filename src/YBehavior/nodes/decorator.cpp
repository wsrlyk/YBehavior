#include "YBehavior/nodes/decorator.h"
#include "YBehavior/profile/profileheader.h"

namespace YBehavior
{
	NodeState AlwaysSuccess::Update(AgentPtr pAgent)
	{
		PROFILER_PAUSE;
		NodeState ns = SingleChildNode::Update(pAgent);
		PROFILER_RESUME;
		return ns == NS_RUNNING ? NS_RUNNING : NS_SUCCESS;
	}
	NodeState AlwaysFailure::Update(AgentPtr pAgent)
	{
		PROFILER_PAUSE;
		NodeState ns = SingleChildNode::Update(pAgent);
		PROFILER_RESUME;
		return ns == NS_RUNNING ? NS_RUNNING : NS_FAILURE;
	}

	YBehavior::NodeState Invertor::Update(AgentPtr pAgent)
	{
		PROFILER_PAUSE;
		NodeState ns = SingleChildNode::Update(pAgent);
		PROFILER_RESUME;
		if (ns == NS_RUNNING)
			return NS_RUNNING;
		if (ns == NS_SUCCESS)
			return NS_FAILURE;
		return NS_SUCCESS;
	}

}