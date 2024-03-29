#include "YBehavior/nodes/setdata.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/variablecreation.h"

namespace YBehavior
{
	bool SetData::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		TYPEID leftType = VariableCreation::CreateVariable(this, m_Opl, "Target", data, true);
		if (leftType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Opl in SetData: " << leftType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID rightType = VariableCreation::CreateVariable(this, m_Opr, "Source", data);
		if (leftType != rightType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  Opl & Opr" << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState SetData::Update(AgentPtr pAgent)
	{
		m_Opl->SetValue(pAgent->GetMemory(), m_Opr->GetValue(pAgent->GetMemory()));

		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Opl, false);

		return NS_SUCCESS;
	}
}
