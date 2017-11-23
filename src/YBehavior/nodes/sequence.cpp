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
		NodeState ns = NS_SUCCESS;
		for (auto it = m_Childs->begin(); it != m_Childs->end(); ++it)
		{
			ns = (*it)->Execute(pAgent);
			if (ns == NS_FAILED)
				break;
		}

		return ns;
	}

}