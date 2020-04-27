#include "YBehavior/nodes/sequence.h"
#include "YBehavior/runningcontext.h"
#include "YBehavior/profile/profileheader.h"

namespace YBehavior
{
	NodeState Sequence::Update(AgentPtr pAgent)
	{
		PROFILER_ENABLE_TOTAL;
		NodeState ns = NS_SUCCESS;
		if (m_Childs == nullptr)
			return ns;
		
		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			m_Iterator.Init(m_RCContainer.GetRC()->Current);
			ns = NS_RUNNING;
		}
		else
		{
			m_Iterator.Init(0);
		}
		
		for (int i = m_Iterator.GetStart(); i < (int)m_Childs->size(); ++i)
		{
			BehaviorNodePtr node = (*m_Childs)[m_Iterator.GetIndex(i)];
			PROFILER_PAUSE;
			ns = node->Execute(pAgent, ns);
			PROFILER_RESUME;
			switch (ns)
			{
			case YBehavior::NS_FAILURE:
				DEBUG_LOG_INFO("Break At Child With UID " << Utility::ToString(node->GetUID()) << "; ");
				return ns;
				break;
			case YBehavior::NS_RUNNING:
				m_RCContainer.CreateRC(this);
				m_RCContainer.GetRC()->Current = i;
				return ns;
				break;
			default:
				break;
			}
		}

		return ns;
	}

	NodeState RandomSequence::Update(AgentPtr pAgent)
	{
		PROFILER_ENABLE_TOTAL;
		NodeState ns = NS_SUCCESS;
		if (m_Childs == nullptr)
			return ns;

		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			m_Iterator.Init(m_RCContainer.GetRC()->Current);
			m_Iterator.SetIndexList(m_RCContainer.GetRC()->IndexList);
			ns = NS_RUNNING;
		}
		else
		{
			m_RandomIndex.Rand();
			m_Iterator.Init(0);
			m_Iterator.SetIndexList(m_RandomIndex.GetIndexList());
		}

		DEBUG_LOG_INFO("Order: ")
		for (int i = m_Iterator.GetStart(); i < (int)m_Childs->size(); ++i)
		{
			int index = m_Iterator.GetIndex(i);
			DEBUG_LOG_INFO(index << ", ")
			
			BehaviorNodePtr node = (*m_Childs)[index];

			PROFILER_PAUSE;
			ns = node->Execute(pAgent, ns);
			PROFILER_RESUME;
			switch (ns)
			{
			case YBehavior::NS_FAILURE:
				DEBUG_LOG_INFO("Break At Child With UID " << Utility::ToString(node->GetUID()) << "; ");
				return ns;
				break;
			case YBehavior::NS_RUNNING:
				m_RCContainer.CreateRC(this);
				m_RCContainer.GetRC()->Current = i;
				m_RCContainer.GetRC()->IndexList = (m_RandomIndex.GetIndexList());
				return ns;
				break;
			default:
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