#include "YBehavior/nodes/switchcase.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#include "YBehavior/utility.h"
#endif // DEBUGGER
#include "YBehavior/agent.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetClassTypeNumberId<Int>(),
		GetClassTypeNumberId<Float>(),
		GetClassTypeNumberId<Bool>(),
		GetClassTypeNumberId<String>(),
		GetClassTypeNumberId<Uint64>(),
	};

	NodeState SwitchCase::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_FAILURE;

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Switch, true);
		}

		INT size = m_CasesChilds.size();

		IVariableOperationHelper* pHelper = m_Switch->GetOperation();

		for (INT i = 0; i < size; ++i)
		{
			const void* onecase = m_Cases->GetElement(pAgent->GetSharedData(), i);
			if (onecase == nullptr)
				continue;

			if (pHelper->Compare(pAgent->GetSharedData(), m_Switch->GetValue(pAgent->GetSharedData()), onecase, OT_EQUAL))
			{
				DEBUG_LOG_INFO("Switch to case " << Utility::ToString(m_CasesChilds[i]->GetUID()) << "; ");

				ns = m_CasesChilds[i]->Execute(pAgent);
				return ns;
			}
		}

		if (m_DefaultChild != nullptr)
		{
			DEBUG_LOG_INFO("Switch to default; ");
			ns = m_DefaultChild->Execute(pAgent);
		}
		return ns;
	}

	void SwitchCase::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID switchType = CreateVariable(m_Switch, "Switch", data, true);
		if (s_ValidTypes.find(switchType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Switch in SwitchCase: " << switchType << ERROR_END;
			return;
		}
		TYPEID casesType = CreateVariable(m_Cases, "Cases", data, false);
		if (s_ValidTypes.find(casesType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Cases in SwitchCase: " << casesType << ERROR_END;
			return;
		}

		if (!Utility::IsElement(switchType, casesType))
		{
			ERROR_BEGIN << "Different types in SwitchCase:  " << switchType << " and " << casesType << ERROR_END;
		}
	}

	void SwitchCase::OnLoadFinish()
	{
		if (m_CasesChilds.size() != m_Cases->VectorSize(nullptr))
		{
			ERROR_BEGIN << "Cases size not match in SwitchCase" << ERROR_END;
		}
	}

	void SwitchCase::OnAddChild(BehaviorNode * child, const STRING & connection)
	{
		if (connection == "default")
		{
			if (m_DefaultChild != nullptr)
			{
				ERROR_BEGIN << "Too many default case in SwitchCase" << ERROR_END;
			}
			else
			{
				m_DefaultChild = child;
			}
		}
		else
		{
			m_CasesChilds.push_back(child);
		}
	}

}