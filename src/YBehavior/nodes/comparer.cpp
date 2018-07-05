#include "YBehavior/nodes/comparer.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER


namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetClassTypeNumberId<Int>(),
		GetClassTypeNumberId<Float>(),
		GetClassTypeNumberId<Bool>(),
		GetClassTypeNumberId<String>(),
		GetClassTypeNumberId<Vector3>(),
		GetClassTypeNumberId<Uint64>()
	};

	void Comparer::OnLoaded(const pugi::xml_node& data)
	{
		///> Operator
		auto attrOptr = data.attribute("Operator");
		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Comparer Operator: " << data.name() << ERROR_END;
			return;
		}
		STRING tempChar = GetValue("Operator", data);
		if (!IVariableOperationHelper::s_OperatorMap.TryGetKey(tempChar, m_Operator))
		{
			ERROR_BEGIN << "Operator Error: " << tempChar << ERROR_END;
			return;
		}

		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = CreateVariable(m_Opl, "Opl", data, true, Utility::POINTER_CHAR);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return;
		}
		///> Right
		TYPEID dataType = CreateVariable(m_Opr, "Opr", data, true);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState Comparer::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opl, true);
			LOG_SHARED_DATA(m_Opr, true);
		}

		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		return pHelper->Compare(pAgent->GetSharedData(), m_Opl, m_Opr, m_Operator) ? NS_SUCCESS : NS_FAILURE;
	}

}
