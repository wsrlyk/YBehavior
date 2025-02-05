#include "unaryoperation.h"
#include "YBehavior/agent.h"
#include "YBehavior/pincreation.h"

namespace YBehavior
{
	static Bimap<UnaryOpType, STRING> OperatorMap = {
		{ UnaryOpType::ABS, "ABS" },
		};

	//////////////////////////////////////////////////////////////////////////////////////////
	bool UnaryOperation::OnLoaded(const pugi::xml_node& data)
	{
		///> Operator
		if (!PinCreation::GetValue(this, "Operator", data, OperatorMap, m_Operator))
			return false;

		//////////////////////////////////////////////////////////////////////////
		///> Left
		auto outputType = PinCreation::CreatePin(this, m_Output, "Output", data, PinCreation::Flag::IsOutput);

		///> Right1
		auto inputType = PinCreation::CreatePin(this, m_Input, "Input", data);

		if (outputType != inputType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Input & Output" << ERROR_END;
			return false;
		}
		m_pHelper = DataUnaryOpMgr::Instance()->Get(outputType);
		if (!m_pHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "These types are not supported by UnaryOperationNode." << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState UnaryOperation::Update(AgentPtr pAgent)
	{
		m_pHelper->Operate(pAgent->GetMemory(), m_Output, m_Input, m_Operator);
		return NS_SUCCESS;
	}

}
