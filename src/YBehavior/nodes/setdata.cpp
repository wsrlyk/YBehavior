#include "YBehavior/nodes/setdata.h"
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
		GetClassTypeNumberId<String>(),
		GetClassTypeNumberId<Vector3>(),
		GetClassTypeNumberId<Uint64>()
	};

	void SetData::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = CreateVariable(m_Opl, "Target", data, true, Utility::POINTER_CHAR);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return;
		}
		///> Right
		TYPEID dataType = CreateVariable(m_Opr, "Source", data, true);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return;
		}
	}

	YBehavior::NodeState SetData::Update(AgentPtr pAgent)
	{
		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Opl, true);

		m_Opl->SetValue(pAgent->GetSharedData(), m_Opr->GetValue(pAgent->GetSharedData()));

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Opl, false);

		return NS_SUCCESS;
	}
}
