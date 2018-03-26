#include "YBehavior/nodes/comparer.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/Agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	std::unordered_set<INT> Comparer::s_ValidTypes = {
		GetClassTypeNumberId<Int>(),
		GetClassTypeNumberId<Float>(),
		GetClassTypeNumberId<Bool>(),
		GetClassTypeNumberId<String>(),
		GetClassTypeNumberId<Vector3>(),
		GetClassTypeNumberId<Uint64>()
	};

	void Comparer::OnLoaded(const pugi::xml_node& data)
	{
		///> ÔËËã·û
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
		///> µÈºÅ×ó±ß
		m_DataType = CreateVariable(m_Opl, "Opl", data, true);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return;
		}
		///> µÈºÅÓÒ±ß1
		INT dataType = CreateVariable(m_Opr, "Opr", data, true);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState Comparer::Update(AgentPtr pAgent)
	{
		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		return pHelper->Compare(pAgent->GetSharedData(), m_Opl, m_Opr, m_Operator) ? NS_SUCCESS : NS_FAILED;
	}

}