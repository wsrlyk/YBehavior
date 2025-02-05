#include "convert.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/pincreation.h"
#include "../operations/dataconvert.h"

namespace YBehavior
{
	bool Convert::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		TYPEID leftType = PinCreation::CreatePin(this, m_Source, "Source", data);
		if (leftType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Source in Convert: " << leftType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID rightType = PinCreation::CreatePin(this, m_Target, "Target", data, PinCreation::Flag::IsOutput);
		if (rightType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Target in Convert: " << rightType << ERROR_END;
			return false;
		}

		m_pConvert = DataConvertMgr::Instance()->GetConvert(leftType, rightType);
		if (!m_pConvert)
		{
			ERROR_BEGIN_NODE_HEAD << "Cant convert from type " << leftType << " to type " << rightType << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState Convert::Update(AgentPtr pAgent)
	{
		if (!m_pConvert->Convert(pAgent->GetMemory(), m_Source, m_Target))
			return NS_FAILURE;

		return NS_SUCCESS;
	}
}
