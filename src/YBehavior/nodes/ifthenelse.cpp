#include "ifthenelse.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/fsm/context.h"

namespace YBehavior
{
	void IfThenElse::OnAddChild(TreeNode * child, const STRING & connection)
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

	NodeState IfThenElseNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		IfThenElse* pNode = (IfThenElse*)m_pNode;
		switch ((IfThenElsePhase)m_Stage)
		{
		case IfThenElsePhase::None:
			if (!pNode->m_If)
				return NS_FAILURE;
			pAgent->GetTreeContext()->PushCallStack(pNode->m_If->CreateContext());
			++m_Stage;
			break;
		case IfThenElsePhase::If:
			if (lastState == NS_SUCCESS)
			{
				if (!pNode->m_Then)
					return NS_FAILURE;
				pAgent->GetTreeContext()->PushCallStack(pNode->m_Then->CreateContext());
			}
			else
			{
				if (!pNode->m_Else)
					return NS_FAILURE;
				pAgent->GetTreeContext()->PushCallStack(pNode->m_Else->CreateContext());
			}
			++m_Stage;
			break;
		case IfThenElsePhase::ThenElse:
			return lastState;
		default:
			break;
		}

		return NS_RUNNING;
	}
}