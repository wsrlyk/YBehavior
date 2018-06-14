#include "YBehavior/nodes/register.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER
#include "YBehavior/registerdata.h"

namespace YBehavior
{
	void ReadRegister::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeID;
		if ((typeID = CreateVariable(m_Event, "Event", data, true, POINTER)) != GetClassTypeNumberId<String>())
		{
			ERROR_BEGIN << "Invalid type for Event in ReadRegister " << typeID << ERROR_END;
			return;
		}

		if ((typeID = CreateVariable(m_Int, "Int", data, false, POINTER)) != GetClassTypeNumberId<VecInt>())
		{
			ERROR_BEGIN << "Invalid type for Event in ReadRegister " << typeID << ERROR_END;
			return;
		}

		if ((typeID = CreateVariable(m_Float, "Float", data, false, POINTER)) != GetClassTypeNumberId<VecFloat>())
		{
			ERROR_BEGIN << "Invalid type for Float in ReadRegister " << typeID << ERROR_END;
			return;
		}

		if ((typeID = CreateVariable(m_Bool, "Bool", data, false, POINTER)) != GetClassTypeNumberId<VecBool>())
		{
			ERROR_BEGIN << "Invalid type for Bool in ReadRegister " << typeID << ERROR_END;
			return;
		}

		if ((typeID = CreateVariable(m_Ulong, "Ulong", data, false, POINTER)) != GetClassTypeNumberId<VecUint64>())
		{
			ERROR_BEGIN << "Invalid type for Ulong in ReadRegister " << typeID << ERROR_END;
			return;
		}

		if ((typeID = CreateVariable(m_String, "String", data, false, POINTER)) != GetClassTypeNumberId<VecString>())
		{
			ERROR_BEGIN << "Invalid type for String in ReadRegister " << typeID << ERROR_END;
			return;
		}
	}

	NodeState ReadRegister::Update(AgentPtr pAgent)
	{
		RegisterData* pRegister = pAgent->GetRegister();
		if (!pRegister->IsDirty())
		{
			DEBUG_LOG_INFO("No Event.");
			return NS_FAILURE;
		}
		m_Event->SetValue(pAgent->GetSharedData(), pRegister->GetEvent());
		m_Int->SetValue(pAgent->GetSharedData(), pRegister->GetInt());
		m_Float->SetValue(pAgent->GetSharedData(), pRegister->GetFloat());
		m_Bool->SetValue(pAgent->GetSharedData(), pRegister->GetBool());
		m_Ulong->SetValue(pAgent->GetSharedData(), pRegister->GetUlong());
		m_String->SetValue(pAgent->GetSharedData(), pRegister->GetString());

		pRegister->Clear();

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Event, false);
			LOG_SHARED_DATA(m_Int, false);
			LOG_SHARED_DATA(m_Float, false);
			LOG_SHARED_DATA(m_String, false);
			LOG_SHARED_DATA(m_Bool, false);
			LOG_SHARED_DATA(m_Ulong, false);
		}

		return NS_SUCCESS;
	}
}
