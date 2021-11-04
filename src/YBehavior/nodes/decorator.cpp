#include "YBehavior/nodes/decorator.h"
#include "YBehavior/profile/profileheader.h"
#include "YBehavior/variablecreation.h"
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
		VariableCreation::CreateVariable(this, m_Output, "Output", data, true);

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
			pNode->m_Output->SetCastedValue(pAgent->GetMemory(), Utility::TRUE_VALUE);
		else
			pNode->m_Output->SetCastedValue(pAgent->GetMemory(), Utility::FALSE_VALUE);

		return ns;
	}
}