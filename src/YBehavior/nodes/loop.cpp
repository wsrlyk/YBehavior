#include "YBehavior/nodes/loop.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/agent.h"
#include "YBehavior/profile/profileheader.h"

namespace YBehavior
{
	bool For::OnLoaded(const pugi::xml_node& data)
	{
		CreateVariableIfExist(m_ExitValue, "ExitValue", data);

		return true;
	}

	void For::OnAddChild(TreeNode * child, const STRING & connection)
	{
		if (connection == "init")
		{
			if (m_InitChild != nullptr)
			{
				ERROR_BEGIN_NODE_HEAD << "Too many init" << ERROR_END;
			}
			else
			{
				m_InitChild = child;
			}
		}
		else if (connection == "cond")
		{
			if (m_CondChild != nullptr)
			{
				ERROR_BEGIN_NODE_HEAD << "Too many cond" << ERROR_END;
			}
			else
			{
				m_CondChild = child;
			}
		}
		else if (connection == "inc")
		{
			if (m_IncChild != nullptr)
			{
				ERROR_BEGIN_NODE_HEAD << "Too many inc" << ERROR_END;
			}
			else
			{
				m_IncChild = child;
			}
		}
		else
		{
			if (m_MainChild != nullptr)
			{
				ERROR_BEGIN_NODE_HEAD << "Too many children" << ERROR_END;
			}
			else
			{
				m_MainChild = child;
			}
		}
	}

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////


	bool ForEach::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID collectionType = CreateVariable(m_Collection, "Collection", data);
		TYPEID currentType = CreateVariable(m_Current, "Current", data);
		if (!Utility::IsElement(currentType, collectionType))
		{
			ERROR_BEGIN_NODE_HEAD << "Types not match: " << currentType << " and " << collectionType << ERROR_END;
			return false;
		}

		CreateVariableIfExist(m_ExitValue, "ExitValue", data);

		return true;
	}

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////

	bool Loop::OnLoaded(const pugi::xml_node& data)
	{
		CreateVariable(m_Current, "Current", data);
		if (!m_Current)
		{
			return false;
		}
		CreateVariable(m_Count, "Count", data);
		if (!m_Count)
		{
			return false;
		}

		CreateVariableIfExist(m_ExitValue, "ExitValue", data);

		return true;
	}

	void ForNodeContext::_OnInit()
	{
		CompositeNodeContext::_OnInit();
		m_LoopTimes = 0;
	}

	NodeState ForNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		For* pNode = (For*)m_pNode;
		while (true)
		{
			if ((ForPhase)m_Stage == ForPhase::None)
			{
				if (pNode->m_CondChild == nullptr && pNode->m_MainChild == nullptr)
				{
					YB_LOG_INFO_WITH_END("At least one of Cond and Main Child must be configured.")
					return NS_FAILURE;
				}
				m_Stage = (int)ForPhase::Init;
				if (pNode->m_InitChild)
				{
					pAgent->GetTreeContext()->PushCallStack(pNode->m_InitChild->CreateContext());
					return NS_RUNNING;
				}
			}
			if ((ForPhase)m_Stage == ForPhase::Init || (ForPhase)m_Stage == ForPhase::Inc)
			{
				m_Stage = (int)ForPhase::Cond;
				if (pNode->m_CondChild)
				{
					pAgent->GetTreeContext()->PushCallStack(pNode->m_CondChild->CreateContext());
					return NS_RUNNING;
				}
			}
			if ((ForPhase)m_Stage == ForPhase::Cond)
			{
				if (pNode->m_CondChild && lastState == NS_FAILURE)
				{
					if (pNode->m_ExitValue)
					{
						return NS_FAILURE;
					}
					else
					{
						return NS_SUCCESS;
					}
				}
				m_Stage = (int)ForPhase::Main;
				if (pNode->m_MainChild)
				{
					pAgent->GetTreeContext()->PushCallStack(pNode->m_MainChild->CreateContext());
					return NS_RUNNING;
				}
			}
			if ((ForPhase)m_Stage == ForPhase::Main)
			{
				++m_LoopTimes;
				if (pNode->m_MainChild && pNode->m_ExitValue)
				{
					BOOL bExit = Utility::FALSE_VALUE;
					pNode->m_ExitValue->GetCastedValue(pAgent->GetMemory(), bExit);

					if ((bExit && lastState == NS_SUCCESS)
						||(!bExit && lastState == NS_FAILURE))
					return NS_SUCCESS;
				}
				m_Stage = (int)ForPhase::Inc;
				if (pNode->m_IncChild)
				{
					pAgent->GetTreeContext()->PushCallStack(pNode->m_IncChild->CreateContext());
					return NS_RUNNING;
				}
			}
		}

		return NS_FAILURE;
	}

	NodeState ForEachNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		ForEach* pNode = (ForEach*)m_pNode;
		if (!pNode->m_Child)
		{
			YB_LOG_INFO_WITH_END("No Child.");
			return NS_FAILURE;
		}

		INT size = pNode->m_Collection->VectorSize(pAgent->GetMemory());
		if (m_Stage > 0)
		{
			if (pNode->m_ExitValue)
			{
				BOOL bExit = Utility::FALSE_VALUE;
				pNode->m_ExitValue->GetCastedValue(pAgent->GetMemory(), bExit);

				if ((bExit && lastState == NS_SUCCESS)
					|| (!bExit && lastState == NS_FAILURE))
					return NS_SUCCESS;
			}
		}
		if (m_Stage < size)
		{
			const void* element = pNode->m_Collection->GetElement(pAgent->GetMemory(), m_Stage);
			if (element != nullptr)
			{
				pNode->m_Current->SetValue(pAgent->GetMemory(), element);
			}
			++m_Stage;
			//if (pNode->m_Child)
			{
				pAgent->GetTreeContext()->PushCallStack(pNode->m_Child->CreateContext());
				return NS_RUNNING;
			}
		}
		else
		{
			return NS_SUCCESS;
		}

		return NS_FAILURE;
	}

	NodeState LoopNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		Loop* pNode = (Loop*)m_pNode;
		if (!pNode->m_Child)
		{
			YB_LOG_INFO_WITH_END("No Child.");
			return NS_FAILURE;
		}
		INT size = 0;
		pNode->m_Count->GetCastedValue(pAgent->GetMemory(), size);
		if (m_Stage > 0)
		{
			if (pNode->m_ExitValue)
			{
				BOOL bExit = Utility::FALSE_VALUE;
				pNode->m_ExitValue->GetCastedValue(pAgent->GetMemory(), bExit);

				if ((bExit && lastState == NS_SUCCESS)
					|| (!bExit && lastState == NS_FAILURE))
					return NS_SUCCESS;
			}
		}
		if (m_Stage < size)
		{
			pNode->m_Current->SetCastedValue(pAgent->GetMemory(), &m_Stage);

			++m_Stage;
			//if (pNode->m_Child)
			{
				pAgent->GetTreeContext()->PushCallStack(pNode->m_Child->CreateContext());
				return NS_RUNNING;
			}
		}
		else
		{
			return NS_SUCCESS;
		}

		return NS_FAILURE;
	}

}