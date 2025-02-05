#include "decorator.h"
#include "YBehavior/pincreation.h"
#include "YBehavior/agent.h"

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

	bool ConvertToBool::OnLoaded(const pugi::xml_node& data)
	{
		PinCreation::CreatePin(this, m_Output, "Output", data, PinCreation::Flag::IsOutput);

		if (!m_Output)
			return false;

		return true;
	}

	YBehavior::NodeState ConvertToBoolNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		NodeState ns = SingleChildNodeContext::_Update(pAgent, lastState);
		if (ns == NS_RUNNING || ns == NS_BREAK)
			return ns;

		ConvertToBool* pNode = (ConvertToBool*)m_pNode;
		if (ns == NS_SUCCESS)
			pNode->m_Output->SetValue(pAgent->GetMemory(), Utility::TRUE_VALUE);
		else
			pNode->m_Output->SetValue(pAgent->GetMemory(), Utility::FALSE_VALUE);

		return ns;
	}
}