#include "YBehavior/nodes/convert.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/variablecreation.h"
#include "YBehavior/variableconvert.h"

namespace YBehavior
{
	bool Convert::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		TYPEID leftType = VariableCreation::CreateVariable(this, m_Source, "Source", data, true);
		if (leftType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Source in Convert: " << leftType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID rightType = VariableCreation::CreateVariable(this, m_Target, "Target", data);
		if (rightType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Target in Convert: " << rightType << ERROR_END;
			return false;
		}

		m_pConvert = VariableConvertMgr::Instance()->GetConvert(leftType, rightType);
		if (!m_pConvert)
		{
			ERROR_BEGIN_NODE_HEAD << "Cant convert from type " << leftType << " to type " << rightType << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState Convert::Update(AgentPtr pAgent)
	{
		YB_LOG_VARIABLE_BEFORE_IF_HAS_DEBUG_POINT(m_Source);
		if (!m_pConvert->Convert(pAgent->GetMemory(), m_Source, m_Target))
			return NS_FAILURE;

		YB_LOG_VARIABLE_AFTER_IF_HAS_DEBUG_POINT(m_Target);

		return NS_SUCCESS;
	}
}
