#include "YBehavior/nodes/sequence.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#include "YBehavior/utility.h"
#endif // DEBUGGER

namespace YBehavior
{
	NodeState Sequence::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_SUCCESS;
		if (m_Childs == nullptr)
			return ns;
		for (auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
		{
			ns = (*it)->Execute(pAgent);
			if (ns == NS_FAILURE)
			{
				DEBUG_LOG_INFO("Break At Child With UID " << Utility::ToString((*it)->GetUID()) << "; ");
				break;
			}
		}

		return ns;
	}

	NodeState RandomSequence::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_SUCCESS;
		if (m_Childs == nullptr)
			return ns;

		m_RandomIndex.Rand();

		DEBUG_LOG_INFO("Order: ")
		for (size_t i = 0; i < m_Childs->size(); ++i)
		{
			int index = m_RandomIndex[i];
			DEBUG_LOG_INFO(index << ", ")

			ns = (*m_Childs)[index]->Execute(pAgent);
			if (ns == NS_FAILURE)
			{
				DEBUG_LOG_INFO("Break At Child With UID " << Utility::ToString((*m_Childs)[index]->GetUID()) << "; ");
				break;
			}
		}
		return ns;
	}

	void RandomSequence::OnAddChild(BehaviorNode* child, const STRING& connection)
	{
		m_RandomIndex.Append();
	}

}