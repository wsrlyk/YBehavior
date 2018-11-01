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

	static std::unordered_set<TYPEID> s_ValidVecTypes = {
		GetClassTypeNumberId<VecInt>(),
		GetClassTypeNumberId<VecFloat>(),
		GetClassTypeNumberId<VecBool>(),
		GetClassTypeNumberId<VecString>(),
		GetClassTypeNumberId<VecUint64>(),
	};

	NodeState SwitchCase::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Switch, true);
			LOG_SHARED_DATA(m_Cases, true);
		}

		if (m_CasesChilds.size() != m_Cases->VectorSize(pAgent->GetMemory()))
		{
			ERROR_BEGIN << "Cases size not match in SwitchCase" << ERROR_END;
			return NS_FAILURE;
		}

		NodeState ns = NS_FAILURE;
		int current = -2;
		m_RCContainer.ConvertRC(this);

		if (m_RCContainer.GetRC())
		{
			ns = NS_RUNNING;
			current = m_RCContainer.GetRC()->Current;
		}

		INT size = m_CasesChilds.size();
		BehaviorNodePtr targetNode = nullptr;
		switch (current)
		{
		case -2:
		{
			IVariableOperationHelper* pHelper = m_Switch->GetOperation();

			for (INT i = 0; i < size; ++i)
			{
				const void* onecase = m_Cases->GetElement(pAgent->GetMemory(), i);
				if (onecase == nullptr)
					continue;

				if (pHelper->Compare(m_Switch->GetValue(pAgent->GetMemory()), onecase, OT_EQUAL))
				{
					DEBUG_LOG_INFO("Switch to case " << Utility::ToString(m_CasesChilds[i]->GetUID()) << "; ");
					targetNode = m_CasesChilds[i];
					current = i;
					break;
				}
			}
			if (targetNode == nullptr)
			{
				targetNode = m_DefaultChild;
				current = -1;
			}
			break;
		}
		case -1:
		{
			DEBUG_LOG_INFO("Switch to default; ");
			targetNode = m_DefaultChild;
			break;
		}
		default:
		{
			if (0 <= current && current < size)
			{
				DEBUG_LOG_INFO("Switch to case " << Utility::ToString(m_CasesChilds[current]->GetUID()) << "; ");
				targetNode = m_CasesChilds[current];
			}
			break;
		}
		}

		if (targetNode)
		{
			ns = targetNode->Execute(pAgent, ns);
			if (ns == NS_RUNNING)
			{
				m_RCContainer.CreateRC(this);
				m_RCContainer.GetRC()->Current = current;
			}
			return ns;
		}
		return NS_FAILURE;
	}

	bool SwitchCase::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID switchType = CreateVariable(m_Switch, "Switch", data, ST_SINGLE);
		if (s_ValidTypes.find(switchType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Switch in SwitchCase: " << switchType << ERROR_END;
			return false;
		}
		TYPEID casesType = CreateVariable(m_Cases, "Cases", data, ST_ARRAY);
		if (s_ValidVecTypes.find(casesType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Cases in SwitchCase: " << casesType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(switchType, casesType))
		{
			ERROR_BEGIN << "Different types in SwitchCase:  " << switchType << " and " << casesType << ERROR_END;
			return false;
		}

		return true;
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