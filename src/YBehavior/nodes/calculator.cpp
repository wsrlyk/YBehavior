#include "YBehavior/nodes/calculator.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/variablecreation.h"

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
		if (!VariableCreation::GetValue(this, "Operator", data, IVariableOperationHelper::s_OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = VariableCreation::CreateVariable(this, m_Output, "Output", data, true);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Output in calculator: " << m_DataType << ERROR_END;
			return false;
		}
		///> Right1
		TYPEID dataType = VariableCreation::CreateVariable(this, m_Input1, "Input1", data);
		if (dataType != m_DataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Output & Input1" << ERROR_END;
			return false;
		}
		///> Right2
		dataType = VariableCreation::CreateVariable(this, m_Input2, "Input2", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types: Output & Input2" << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState Calculator::Update(AgentPtr pAgent)
	{
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE_BEFORE(m_Input1);
			YB_LOG_VARIABLE_BEFORE(m_Input2);
		}

		IVariableOperationHelper* pHelper = m_Output->GetOperation();
		pHelper->Calculate(pAgent->GetMemory(), m_Output, m_Input1, m_Input2, m_Operator);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE_AFTER(m_Output);
		}

		return NS_SUCCESS;
	}

}
