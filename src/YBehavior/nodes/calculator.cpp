#include "YBehavior/nodes/calculator.h"
#include "YBehavior/agent.h"
#include "YBehavior/variablecreation.h"

namespace YBehavior
{
	static Bimap<CalculateType, STRING> OperatorMap = {
		{ CalculateType::ADD, "+" },
		{ CalculateType::SUB, "-" },
		{ CalculateType::MUL, "*" },
		{ CalculateType::DIV, "/" } };

	//////////////////////////////////////////////////////////////////////////////////////////
	bool Calculator::OnLoaded(const pugi::xml_node& data)
	{
		///> Operator
		if (!VariableCreation::GetValue(this, "Operator", data, OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		auto outputType = VariableCreation::CreateVariable(this, m_Output, "Output", data, true);

		///> Right1
		auto inputType1 = VariableCreation::CreateVariable(this, m_Input1, "Input1", data);

		///> Right2
		auto inputType2 = VariableCreation::CreateVariable(this, m_Input2, "Input2", data);

		m_pHelper = VariableCalculateMgr::Instance()->Get(outputType, inputType1, inputType2);
		if (!m_pHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "These types are not supported by CalculatorNode." << ERROR_END;
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

		m_pHelper->Calculate(pAgent->GetMemory(), m_Output, m_Input1, m_Input2, m_Operator);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE_AFTER(m_Output);
		}

		return NS_SUCCESS;
	}

}
