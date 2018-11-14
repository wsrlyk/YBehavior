#include "YBehavior/nodes/setdata.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	bool SetData::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		TYPEID leftType = CreateVariable(m_Opl, "Target", data, ST_SINGLE, Utility::POINTER_CHAR);
		if (leftType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN << "Invalid type for Opl in SetData: " << leftType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID rightType = CreateVariable(m_Opr, "Source", data, ST_SINGLE);
		if (leftType != rightType)
		{
			ERROR_BEGIN << "Different types:  " << leftType << " and " << rightType << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState SetData::Update(AgentPtr pAgent)
	{
		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Opl, true);

		m_Opl->SetValue(pAgent->GetMemory(), m_Opr->GetValue(pAgent->GetMemory()));

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Opl, false);

		return NS_SUCCESS;
	}

	bool SetArray::OnLoaded(const pugi::xml_node& data)
	{
		///> Left
		TYPEID leftType = CreateVariable(m_Opl, "Target", data, ST_ARRAY, Utility::POINTER_CHAR);
		if (leftType == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN << "Invalid type for Target in SetArray: " << leftType << ERROR_END;
			return false;
		}
		///> Right
		TYPEID rightType = CreateVariable(m_Opr, "Source", data, ST_ARRAY);
		if (leftType != rightType)
		{
			ERROR_BEGIN << "Different types:  " << leftType << " and " << rightType << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState SetArray::Update(AgentPtr pAgent)
	{
		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Opl, true);

		m_Opl->SetValue(pAgent->GetMemory(), m_Opr->GetValue(pAgent->GetMemory()));

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Opl, false);

		return NS_SUCCESS;
	}

}
