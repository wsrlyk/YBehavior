#include "YBehavior/nodes/set.h"
#include "YBehavior/agent.h"
#include "YBehavior/variablecreation.h"

namespace YBehavior
{

	static Bimap<SetOperationType, STRING> OperatorMap = {
		{ SetOperationType::APPEND, "APPEND" },
		{ SetOperationType::MERGE, "MERGE" },
		{ SetOperationType::EXCLUDE, "EXCLUDE" }};

	//////////////////////////////////////////////////////////////////////////////////////////
	bool SetOperation::OnLoaded(const pugi::xml_node& data)
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

		if (!(outputType == inputType1 && outputType == inputType2))
		{
			ERROR_BEGIN_NODE_HEAD << "Types must be the same." << ERROR_END;
			return false;
		}
		m_pHelper = VariableSetOperationMgr::Instance()->Get(outputType);
		if (!m_pHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "These types are not supported." << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState SetOperation::Update(AgentPtr pAgent)
	{
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE_BEFORE(m_Input1);
			YB_LOG_VARIABLE_BEFORE(m_Input2);
		}

		m_pHelper->SetOperation(pAgent->GetMemory(), m_Output, m_Input1, m_Input2, m_Operator);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE_AFTER(m_Output);
		}

		return NS_SUCCESS;
	}

}