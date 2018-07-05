#include "YBehavior/nodes/random.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetClassTypeNumberId<Int>(),
		GetClassTypeNumberId<Float>(),
		GetClassTypeNumberId<Bool>(),
	};

	void Random::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = CreateVariable(m_Opl, "Target", data, true, Utility::POINTER_CHAR);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return;
		}
		///> Right1
		TYPEID dataType = CreateVariable(m_Opr1, "Bound1", data, true);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return;
		}
		///> Right2
		dataType = CreateVariable(m_Opr2, "Bound2", data, true);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState Random::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opr1, true);
			LOG_SHARED_DATA(m_Opr2, true);
			LOG_SHARED_DATA(m_Opl, true);
		}

		IVariableOperationHelper* pHelper = m_Opl->GetOperation();
		pHelper->Random(pAgent->GetSharedData(), m_Opl, m_Opr1, m_Opr2);

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opl, false);
		}

		return NS_SUCCESS;
	}
}
