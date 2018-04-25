#include "YBehavior/nodes/selector.h"

namespace YBehavior
{
	YBehavior::NodeState Selector::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_FAILED;
		for (auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
		{
			ns = (*it)->Execute(pAgent);
			if (ns == NS_SUCCESS)
				break;
		}

		return ns;
	}

}