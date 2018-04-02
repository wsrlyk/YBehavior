#include "YBehavior/nodes/calculator.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	///> Too lazy to create a file for just this line. Temporarily put it here
	Bimap<OperationType, STRING, EnumClassHash> IVariableOperationHelper::s_OperatorMap = {
		{ OT_ADD, "ADD" },{ OT_SUB, "SUB" },{ OT_MUL, "MUL" },{ OT_DIV, "DIV" },
	{ OT_EQUAL, "==" },{ OT_GREATER, ">" },{ OT_LESS, "<" },{ OT_NOT_EQUAL, "!=" },{ OT_LESS_EQUAL, "<=" },{ OT_GREATER_EQUAL, ">=" }
	};

	//////////////////////////////////////////////////////////////////////////////////////////
	std::unordered_set<INT> Calculator::s_ValidTypes = { GetClassTypeNumberId<Int>(), GetClassTypeNumberId<Float>() };

	void Calculator::OnLoaded(const pugi::xml_node& data)
	{
		///> ‘ÀÀ„∑˚
		auto attrOptr = data.attribute("Operator");
		if (attrOptr.empty())
		{
			ERROR_BEGIN << "Cant Find Calculator Operator: " << data.name() << ERROR_END;
			return;
		}
		STRING tempChar = GetValue("Operator", data);
		if (!IVariableOperationHelper::s_OperatorMap.TryGetKey(tempChar, m_Operator))
		{
			ERROR_BEGIN << "Operator Error: " << tempChar << ERROR_END;
			return;
		}

		//////////////////////////////////////////////////////////////////////////
		///> µ»∫≈◊Û±ﬂ
		m_DataType = CreateVariable(m_Opl, "Opl", data, true);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in calculator: " << m_DataType << ERROR_END;
			return;
		}
		///> µ»∫≈”“±ﬂ1
		INT dataType = CreateVariable(m_Opr1, "Opr1", data, true);
		if (dataType != m_DataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " with " << m_DataType << ERROR_END;
			return;
		}
		///> µ»∫≈”“±ﬂ2
		dataType = CreateVariable(m_Opr2, "Opr2", data, true);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " with " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState Calculator::Update(AgentPtr pAgent)
	{
		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		pHelper->Calculate(pAgent->GetSharedData(), m_Opl, m_Opr1, m_Opr2, m_Operator);

		return NS_SUCCESS;
	}

}
