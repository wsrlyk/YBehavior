#include "YBehavior/nodes/sequence.h"

namespace YBehavior
{
	Sequence::Sequence()
	{
	}


	Sequence::~Sequence()
	{
	}

	YBehavior::NodeState Sequence::Update(AgentPtr pAgent)
	{
		for (auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
		{
			m_State = (*it)->Execute(pAgent);
			if (m_State == NS_FAILED)
				break;
		}

		return m_State;
	}

}