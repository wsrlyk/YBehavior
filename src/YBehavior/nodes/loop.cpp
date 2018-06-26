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
		int loopTimes = 0;
		if (m_InitChild != nullptr)
		{
			m_InitChild->Execute(pAgent);
		}

		while (true)
		{
			if (m_CondChild != nullptr && m_CondChild->Execute(pAgent) == NS_FAILURE)
			{
				DEBUG_LOG_INFO("End For at " << loopTimes << " times; ");
				break;
			}

			++loopTimes;

			if (m_MainChild != nullptr)
			{
				NodeState ns = m_MainChild->Execute(pAgent);
				if (ns == NS_FAILURE && *m_ExitWhenFailure->GetCastedValue(pAgent->GetSharedData()))
				{
					DEBUG_LOG_INFO("ExitWhenFailure at " << loopTimes << " times; ");
					break;
				}
			}

			if (m_IncChild != nullptr)
				m_IncChild->Execute(pAgent);
		}

		return NS_SUCCESS;
	}

	void For::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID type = CreateVariable(m_ExitWhenFailure, "ExitWhenFailure", data, true);
		if (type != GetClassTypeNumberId<Bool>())
		{
			ERROR_BEGIN << "Invalid type for ExitWhenFailure in For: " << type << ERROR_END;
			return;
		}
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
		INT size = m_Collection->VectorSize(pAgent->GetSharedData());

		DEBUG_LOG_INFO("Collection: " << m_Collection->GetValueToSTRING(pAgent->GetSharedData()) << "; ");

		for (INT i = 0; i < size; ++i)
		{
			const void* element = m_Collection->GetElement(pAgent->GetSharedData(), i);
			if (element == nullptr)
				continue;

			m_Current->SetValue(pAgent->GetSharedData(), element);

			if (m_Child != nullptr)
			{
				NodeState ns = m_Child->Execute(pAgent);
				if (ns == NS_FAILURE && *m_ExitWhenFailure->GetCastedValue(pAgent->GetSharedData()))
				{
					DEBUG_LOG_INFO("ExitWhenFailure at " << m_Current->GetValueToSTRING(pAgent->GetSharedData()) << "; ");
					break;
				}
			}
		}

		return NS_SUCCESS;
	}

	void ForEach::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID collectionType = CreateVariable(m_Collection, "Collection", data, false);
		TYPEID currentType = CreateVariable(m_Current, "Current", data, true);
		if (!Utility::IsElement(currentType, collectionType))
		{
			ERROR_BEGIN << "Types not match in ForEach: " << currentType << " and " << collectionType << ERROR_END;
			return;
		}

		TYPEID type = CreateVariable(m_ExitWhenFailure, "ExitWhenFailure", data, true);
		if (type != GetClassTypeNumberId<Bool>())
		{
			ERROR_BEGIN << "Invalid type for ExitWhenFailure in For: " << type << ERROR_END;
			return;
		}
	}
}