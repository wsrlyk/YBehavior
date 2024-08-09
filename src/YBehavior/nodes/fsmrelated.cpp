#include "YBehavior/nodes/fsmrelated.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/pincreation.h"
#include "YBehavior/fsm/context.h"

namespace YBehavior
{
	bool FSMSetCondition::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		TYPEID type = PinCreation::CreatePin(this, m_Conditions, "Conditions", data);
		if (type == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Conditions in FSMSetCondition: " << type << ERROR_END;
			return false;
		}

		STRING op(PinCreation::GetValue(this, "Operator", data));
		if (op == "On")
			m_IsOn = true;
		else if (op == "Off")
			m_IsOn = false;
		else
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid value for Operator in FSMSetCondition: " << op << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState FSMSetCondition::Update(AgentPtr pAgent)
	{
		YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Conditions, true);


		if (m_Conditions->IsThisArray())
		{
			auto size = m_Conditions->ArraySize(pAgent->GetMemory());
			for (INT i = 0; i < size; ++i)
			{
				auto s = m_Conditions->GetElementPtr(pAgent->GetMemory(), i);
				if (m_IsOn)
					pAgent->GetMachineContext()->GetTransition().Set(*static_cast<const STRING*>(s));
				else
					pAgent->GetMachineContext()->GetTransition().UnSet(*static_cast<const STRING*>(s));
			}
		}
		else
		{
			auto s = m_Conditions->GetValuePtr(pAgent->GetMemory());
			if (m_IsOn)
				pAgent->GetMachineContext()->GetTransition().Set(*static_cast<const STRING*>(s));
			else
				pAgent->GetMachineContext()->GetTransition().UnSet(*static_cast<const STRING*>(s));
		}

		return NS_SUCCESS;
	}

	YBehavior::NodeState FSMClearConditions::Update(AgentPtr pAgent)
	{
		pAgent->GetMachineContext()->GetTransition().Reset();
		return NS_SUCCESS;
	}

}
