#include "YBehavior/nodes/setdata.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/pin.h"
#include "YBehavior/pincreation.h"

namespace YBehavior
{
	bool SetData::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		TYPEID leftType = PinCreation::CreatePin(this, m_Opl, "Target", data, true);
		if (leftType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Opl in SetData: " << leftType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID rightType = PinCreation::CreatePin(this, m_Opr, "Source", data);
		if (leftType != rightType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Opl & Opr" << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState SetData::Update(AgentPtr pAgent)
	{
		m_Opl->SetValue(pAgent->GetMemory(), m_Opr->GetValuePtr(pAgent->GetMemory()));

		YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Opl, false);

		return NS_SUCCESS;
	}
}
