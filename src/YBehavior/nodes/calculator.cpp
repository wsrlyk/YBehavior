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
		{ OT_ADD, "+" },{ OT_SUB, "-" },{ OT_MUL, "*" },{ OT_DIV, "/" },
	{ OT_EQUAL, "==" },{ OT_GREATER, ">" },{ OT_LESS, "<" },{ OT_NOT_EQUAL, "!=" },{ OT_LESS_EQUAL, "<=" },{ OT_GREATER_EQUAL, ">=" }
	};

	//////////////////////////////////////////////////////////////////////////////////////////
	static std::unordered_set<TYPEID> s_ValidTypes = { GetTypeID<Int>(), GetTypeID<Float>(), GetTypeID<String>()};

	bool Calculator::OnLoaded(const pugi::xml_node& data)
	{
		///> Operator
		if (!GetValue("Operator", data, IVariableOperationHelper::s_OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = CreateVariable(m_Opl, "Opl", data, Utility::POINTER_CHAR);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Opl in calculator: " << m_DataType << ERROR_END;
			return false;
		}
		///> Right1
		TYPEID dataType = CreateVariable(m_Opr1, "Opr1", data);
		if (dataType != m_DataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Opl & Opr1" << ERROR_END;
			return false;
		}
		///> Right2
		dataType = CreateVariable(m_Opr2, "Opr2", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types: Opl & Opr2" << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState Calculator::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opl, true);
			LOG_SHARED_DATA(m_Opr1, true);
			LOG_SHARED_DATA(m_Opr2, true);
		}

		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		pHelper->Calculate(pAgent->GetMemory(), m_Opl, m_Opr1, m_Opr2, m_Operator);

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opl, false);
		}

		return NS_SUCCESS;
	}

}
