#include "sequence.h"
#include "YBehavior/agent.h"
#include "YBehavior/fsm/context.h"

namespace YBehavior
{
	void SequenceNodeContext::_OnInit()
	{
		CompositeNodeContext::_OnInit();
		if (m_pChildren)
			m_Iterator.Init((int)m_pChildren->size(), 0);
	}

	YBehavior::NodeState SequenceNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		if (m_Stage == 0)
		{
			++m_Stage;
			if (!m_pChildren)
				return NS_FAILURE;
		}
		else
		{
			///> A child has run. The state must be checked
			if (lastState != NS_SUCCESS)
			{
				YB_LOG_INFO_WITH_END("Break at UID  " << (*m_pChildren)[m_Iterator.Current()]->GetUID());
				++m_Stage;
				return NS_FAILURE;
			}
		}

		if (m_Iterator.MoveNext())
		{
			TreeNodePtr node = (*m_pChildren)[m_Iterator.Current()];
			pAgent->GetTreeContext()->PushCallStack(node->CreateContext());
			return NS_RUNNING;
		}
		++m_Stage;
		return NS_SUCCESS;
	}

	void RandomSequenceNodeContext::_OnInit()
	{
		SequenceNodeContext::_OnInit();
		if (m_pChildren)
		{
			m_RandomIndex.Set((int)m_pChildren->size());
			m_RandomIndex.Rand();
			m_Iterator.SetIndexList(m_RandomIndex.GetIndexList());
		}
	}

}