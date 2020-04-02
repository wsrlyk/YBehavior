#include "YBehavior/nodes/ifthenelse.h"
#include "YBehavior/logger.h"

namespace YBehavior
{
	IfThenElse::IfThenElse()
		: m_If(nullptr)
		, m_Then(nullptr)
		, m_Else(nullptr)
	{
		SetRCCreator(&m_RCContainer);
	}


	IfThenElse::~IfThenElse()
	{
	}

	NodeState IfThenElse::Update(AgentPtr pAgent)
	{
		if (m_If == nullptr)
			return NS_FAILURE;

		NodeState ns = NS_INVALID;
		IfThenElsePhase itep = ITE_Normal;

		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			ns = NS_RUNNING;
			itep = m_RCContainer.GetRC()->Current;
		}

		if (itep == ITE_Normal || itep == ITE_If)
		{
			ns = m_If->Execute(pAgent, ns);
			if (_CheckRunningNodeState(ITE_If, ns))
				return ns;
			itep = ITE_Normal;
		}

		if (ns == NS_SUCCESS || itep == ITE_Then)
		{
			if (m_Then)
			{
				DEBUG_LOG_INFO("Run [THEN]; ");
				ns = m_Then->Execute(pAgent, ns);
				_CheckRunningNodeState(ITE_Then, ns);
				return ns;
			}
			return NS_FAILURE;
		}
		else if (ns == NS_FAILURE || itep == ITE_Else)
		{
			if (m_Else)
			{
				DEBUG_LOG_INFO("Run [ELSE]; ");
				ns = m_Else->Execute(pAgent, ns);
				_CheckRunningNodeState(ITE_Else, ns);
				return ns;
			}
			return NS_FAILURE;
		}
		return NS_FAILURE;
	}

	void IfThenElse::OnAddChild(BehaviorNode * child, const STRING & connection)
	{
		if (connection == "if")
		{
			if (m_If == nullptr)
				m_If = child;
			else
				ERROR_BEGIN_NODE_HEAD << "Too many IF nodes" << ERROR_END;
		}
		else if (connection == "then")
		{
			if (m_Then == nullptr)
				m_Then = child;
			else
				ERROR_BEGIN_NODE_HEAD << "Too many THEN nodes" << ERROR_END;
		}
		else if (connection == "else")
		{
			if (m_Else == nullptr)
				m_Else = child;
			else
				ERROR_BEGIN_NODE_HEAD << "Too many ELSE nodes" << ERROR_END;
		}
		else
		{
			ERROR_BEGIN_NODE_HEAD << "Unknown connection with connection: " << connection << ERROR_END;
		}
	}

	bool IfThenElse::_CheckRunningNodeState(IfThenElsePhase current, NodeState ns)
	{
		if (ns != NS_RUNNING)
			return false;

		m_RCContainer.CreateRC(this);
		m_RCContainer.GetRC()->Current = current;
		return true;
	}

}