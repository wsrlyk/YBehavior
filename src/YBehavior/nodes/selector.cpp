#include "YBehavior/nodes/selector.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER

namespace YBehavior
{
	YBehavior::NodeState Selector::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_FAILED;
		for (auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
		{
			ns = (*it)->Execute(pAgent);
			if (ns == NS_SUCCESS)
			{
#ifdef DEBUGGER
				DEBUG_LOG_INFO("Break At Child With UID " << Utility::ToString((*it)->GetUID()) << "; ");
#endif // DEBUGGER
				break;
			}
		}

		return ns;
	}

}