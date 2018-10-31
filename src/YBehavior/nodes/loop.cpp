#include "YBehavior/nodes/loop.h"
#include "YBehavior/nodefactory.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#include "YBehavior/utility.h"
#endif // DEBUGGER
#include "YBehavior/agent.h"

namespace YBehavior
{
	NodeState For::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_INVALID;
		ForPhase fp = FP_Normal;
		int loopTimes = 0;

		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			ns = NS_RUNNING;
			loopTimes = m_RCContainer.GetRC()->LoopTimes;
			fp = m_RCContainer.GetRC()->Current;
		}

		if (m_InitChild != nullptr && (fp == FP_Normal || fp == FP_Init))
		{
			ns = m_InitChild->Execute(pAgent, ns);
			if (_CheckRunningNodeState(FP_Init, ns, loopTimes))
				return ns;
			fp = FP_Normal;
		}

		while (true)
		{
			if (m_CondChild != nullptr && (fp == FP_Normal || fp == FP_Cond))
			{
				ns = m_CondChild->Execute(pAgent, ns);
				if (_CheckRunningNodeState(FP_Cond, ns, loopTimes))
					return ns;
				fp = FP_Normal;

				if (ns == NS_FAILURE)
				{
					DEBUG_LOG_INFO("End For at " << loopTimes << " times; ");
					break;
				}
			}

			++loopTimes;

			if (m_MainChild != nullptr && (fp == FP_Normal || fp == FP_Main))
			{
				ns = m_MainChild->Execute(pAgent, ns);
				if (_CheckRunningNodeState(FP_Cond, ns, loopTimes))
					return ns;
				fp = FP_Normal;
				if (ns == NS_FAILURE)
				{
					BOOL bExit;
					m_ExitWhenFailure->GetCastedValue(pAgent->GetMemory(), bExit);
					if (bExit)
					{
						DEBUG_LOG_INFO("ExitWhenFailure at " << loopTimes << " times; ");
						break;
					}
				}
			}

			if (m_IncChild != nullptr && (fp == FP_Normal || fp == FP_Inc))
			{
				ns = m_IncChild->Execute(pAgent, ns);
				if (_CheckRunningNodeState(FP_Cond, ns, loopTimes))
					return ns;
				fp = FP_Normal;
			}
		}

		return NS_SUCCESS;
	}

	bool For::_CheckRunningNodeState(ForPhase current, NodeState ns, int looptimes)
	{
		if (ns != NS_RUNNING)
			return false;

		m_RCContainer.CreateRC(this);
		m_RCContainer.GetRC()->Current = current;
		m_RCContainer.GetRC()->LoopTimes = looptimes;
		return true;
	}

	bool For::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID type = CreateVariable(m_ExitWhenFailure, "ExitWhenFailure", data, true);
		if (!m_ExitWhenFailure)
		{
			return false;
		}

		return true;
	}

	void For::OnAddChild(BehaviorNode * child, const STRING & connection)
	{
		if (connection == "init")
		{
			if (m_InitChild != nullptr)
			{
				ERROR_BEGIN << "Too many init in For" << ERROR_END;
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
				ERROR_BEGIN << "Too many cond in For" << ERROR_END;
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
				ERROR_BEGIN << "Too many inc in For" << ERROR_END;
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
				ERROR_BEGIN << "Too many children in For" << ERROR_END;
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

	NodeState ForEach::Update(AgentPtr pAgent)
	{
		INT size = m_Collection->VectorSize(pAgent->GetMemory());
		INT start = 0;
		NodeState ns = NS_INVALID;

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Collection, true);

		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			start = m_RCContainer.GetRC()->Current;
			ns = NS_RUNNING;
		}

		for (INT i = start; i < size; ++i)
		{
			const void* element = m_Collection->GetElement(pAgent->GetMemory(), i);
			if (element == nullptr)
				continue;

			m_Current->SetValue(pAgent->GetMemory(), element);

			if (m_Child != nullptr)
			{
				ns = m_Child->Execute(pAgent, ns);
				switch (ns)
				{
				case YBehavior::NS_FAILURE:
				{
					BOOL bExit;
					m_ExitWhenFailure->GetCastedValue(pAgent->GetMemory(), bExit);
					if (bExit)
					{
						DEBUG_LOG_INFO("ExitWhenFailure at " << m_Current->GetValueToSTRING(pAgent->GetMemory()) << "; ");
						return NS_SUCCESS;
					}
					break;
				}
				case YBehavior::NS_RUNNING:
					m_RCContainer.CreateRC(this);
					m_RCContainer.GetRC()->Current = i;
					return ns;
				default:
					break;
				}
			}
		}

		return NS_SUCCESS;
	}

	bool ForEach::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID collectionType = CreateVariable(m_Collection, "Collection", data, false);
		TYPEID currentType = CreateVariable(m_Current, "Current", data, true);
		if (!Utility::IsElement(currentType, collectionType))
		{
			ERROR_BEGIN << "Types not match in ForEach: " << currentType << " and " << collectionType << ERROR_END;
			return false;
		}

		TYPEID type = CreateVariable(m_ExitWhenFailure, "ExitWhenFailure", data, true);
		if (!m_ExitWhenFailure)
		{
			return false;
		}

		return true;
	}

	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////

	bool Loop::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID type = CreateVariable(m_Current, "Current", data, true);
		if (!m_Current)
		{
			return false;
		}
		type = CreateVariable(m_Count, "Count", data, true);
		if (!m_Count)
		{
			return false;
		}

		type = CreateVariable(m_ExitWhenFailure, "ExitWhenFailure", data, true);
		if (!m_ExitWhenFailure)
		{
			return false;
		}

		return true;
	}

	YBehavior::NodeState Loop::Update(AgentPtr pAgent)
	{
		INT size = 0;
		m_Count->GetCastedValue(pAgent->GetMemory(), size);

		INT start = 0;
		NodeState ns = NS_INVALID;

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Count, true);

		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			start = m_RCContainer.GetRC()->Current;
			ns = NS_RUNNING;
		}

		for (INT i = start; i < size; ++i)
		{
			m_Current->SetCastedValue(pAgent->GetMemory(), &i);

			if (m_Child != nullptr)
			{
				ns = m_Child->Execute(pAgent, ns);
				switch (ns)
				{
				case YBehavior::NS_FAILURE:
				{
					BOOL bExit;
					m_ExitWhenFailure->GetCastedValue(pAgent->GetMemory(), bExit);
					if (bExit)
					{
						DEBUG_LOG_INFO("ExitWhenFailure at " << m_Current->GetValueToSTRING(pAgent->GetMemory()) << "; ");
						return NS_SUCCESS;
					}
					break;
				}
				case YBehavior::NS_RUNNING:
					m_RCContainer.CreateRC(this);
					m_RCContainer.GetRC()->Current = i;
					return ns;
				default:
					break;
				}
			}
		}

		return NS_SUCCESS;
	}
}