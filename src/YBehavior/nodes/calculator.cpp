#include "calculator.h"
#include "YBehavior/agent.h"
#include "YBehavior/pincreation.h"

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
		if (!PinCreation::GetValue(this, "Operator", data, OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		auto outputType = PinCreation::CreatePin(this, m_Output, "Output", data, PinCreation::Flag::IsOutput);

		///> Right1
		auto inputType1 = PinCreation::CreatePin(this, m_Input1, "Input1", data);

		///> Right2
		auto inputType2 = PinCreation::CreatePin(this, m_Input2, "Input2", data);

		m_pHelper = DataCalculateMgr::Instance()->Get(outputType, inputType1, inputType2);
		if (!m_pHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "These types are not supported by CalculatorNode." << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState Calculator::Update(AgentPtr pAgent)
	{
		m_pHelper->Calculate(pAgent->GetMemory(), m_Output, m_Input1, m_Input2, m_Operator);
		return NS_SUCCESS;
	}

}
