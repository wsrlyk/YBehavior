#include "YBehavior/nodes/sequence.h"
#include "YBehavior/debugger.h"

namespace YBehavior
{
	YBehavior::NodeState Sequence::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_SUCCESS;
		for (auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
		{
			ns = (*it)->Execute(pAgent);
			if (ns == NS_FAILED)
			{
				DEBUG_LOG_INFO("Break At Child With UID " << Utility::ToString((*it)->GetUID()) << "; ");
				break;
			}
		}

		return ns;
	}

}