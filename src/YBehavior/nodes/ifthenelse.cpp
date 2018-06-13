#include "YBehavior/nodes/ifthenelse.h"
#include "YBehavior/logger.h"
#include "YBehavior/debugger.h"

namespace YBehavior
{
	IfThenElse::IfThenElse()
		: m_If(nullptr)
		, m_Then(nullptr)
		, m_Else(nullptr)
	{
	}


	IfThenElse::~IfThenElse()
	{
	}

	NodeState IfThenElse::Update(AgentPtr pAgent)
	{
		if (m_If == nullptr)
			return NS_FAILURE;

		NodeState state = m_If->Execute(pAgent);
		if (state == NS_SUCCESS)
		{
			if (m_Then)
			{
				DEBUG_LOG_INFO("Run [THEN]; ");
				return m_Then->Execute(pAgent);
			}
			return NS_FAILURE;
		}
		else
		{
			if (m_Else)
			{
				DEBUG_LOG_INFO("Run [ELSE]; ");
				return m_Else->Execute(pAgent);
			}
			return NS_FAILURE;
		}
	}

	void IfThenElse::OnAddChild(BehaviorNode * child, const STRING & connection)
	{
		if (connection == "if")
		{
			if (m_If == nullptr)
				m_If = child;
			else
				ERROR_BEGIN << "Too many IF nodes for this ifthenelse node: " << GetNodeInfoForPrint() << ERROR_END;
		}
		else if (connection == "then")
		{
			if (m_Then == nullptr)
				m_Then = child;
			else
				ERROR_BEGIN << "Too many THEN nodes for this ifthenelse node: " << GetNodeInfoForPrint() << ERROR_END;
		}
		else if (connection == "else")
		{
			if (m_Else == nullptr)
				m_Else = child;
			else
				ERROR_BEGIN << "Too many ELSE nodes for this ifthenelse node: " << GetNodeInfoForPrint() << ERROR_END;
		}
		else
		{
			ERROR_BEGIN << "Unknown connection for this ifthenelse node: " << GetNodeInfoForPrint() << "with connection: " << connection << ERROR_END;
		}
	}

}