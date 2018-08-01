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
	bool ReadRegister::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeID;
		typeID = CreateVariable(m_Event, "Event", data, true, Utility::POINTER_CHAR);
		if (!m_Event)
		{
			ERROR_BEGIN << "Invalid type for Event in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Int, "Int", data, false, Utility::POINTER_CHAR);
		if (!m_Int)
		{
			ERROR_BEGIN << "Invalid type for Event in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Float, "Float", data, false, Utility::POINTER_CHAR);
		if (!m_Float)
		{
			ERROR_BEGIN << "Invalid type for Float in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Bool, "Bool", data, false, Utility::POINTER_CHAR);
		if (!m_Bool)
		{
			ERROR_BEGIN << "Invalid type for Bool in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Ulong, "Ulong", data, false, Utility::POINTER_CHAR);
		if (!m_Ulong)
		{
			ERROR_BEGIN << "Invalid type for Ulong in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_String, "String", data, false, Utility::POINTER_CHAR);
		if (!m_String)
		{
			ERROR_BEGIN << "Invalid type for String in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		return true;
	}

	NodeState ReadRegister::Update(AgentPtr pAgent)
	{
		RegisterData* pRegister = pAgent->GetRegister();
		if (!pRegister->IsDirty())
		{
			DEBUG_LOG_INFO("No Event.");
			return NS_FAILURE;
		}
		m_Event->SetValue(pAgent->GetSharedData(), pRegister->GetReceiveData().pEvent);
		m_Int->SetValue(pAgent->GetSharedData(), pRegister->GetReceiveData().pVecInt);
		m_Float->SetValue(pAgent->GetSharedData(), pRegister->GetReceiveData().pVecFloat);
		m_Bool->SetValue(pAgent->GetSharedData(), pRegister->GetReceiveData().pVecBool);
		m_Ulong->SetValue(pAgent->GetSharedData(), pRegister->GetReceiveData().pVecUlong);
		m_String->SetValue(pAgent->GetSharedData(), pRegister->GetReceiveData().pVecString);

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


	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	bool WriteRegister::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeID;
		typeID = CreateVariable(m_Event, "Event", data, true);
		if (!m_Event)
		{
			ERROR_BEGIN << "Invalid type for Event in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Int, "Int", data, false);
		if (!m_Int)
		{
			ERROR_BEGIN << "Invalid type for Event in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Float, "Float", data, false);
		if (!m_Float)
		{
			ERROR_BEGIN << "Invalid type for Float in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Bool, "Bool", data, false);
		if (!m_Bool)
		{
			ERROR_BEGIN << "Invalid type for Bool in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_Ulong, "Ulong", data, false);
		if (!m_Ulong)
		{
			ERROR_BEGIN << "Invalid type for Ulong in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		typeID = CreateVariable(m_String, "String", data, false);
		if (!m_String)
		{
			ERROR_BEGIN << "Invalid type for String in ReadRegister " << typeID << ERROR_END;
			return false;
		}

		return true;
	}

	NodeState WriteRegister::Update(AgentPtr pAgent)
	{
		RegisterData* pRegister = pAgent->GetRegister();

		pRegister->GetSendData().pEvent = m_Event->GetCastedValue(pAgent->GetSharedData());
		pRegister->GetSendData().pVecInt = m_Int->GetCastedValue(pAgent->GetSharedData());
		pRegister->GetSendData().pVecFloat = m_Float->GetCastedValue(pAgent->GetSharedData());
		pRegister->GetSendData().pVecBool = m_Bool->GetCastedValue(pAgent->GetSharedData());
		pRegister->GetSendData().pVecUlong = m_Ulong->GetCastedValue(pAgent->GetSharedData());
		pRegister->GetSendData().pVecString = m_String->GetCastedValue(pAgent->GetSharedData());

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Event, true);
			LOG_SHARED_DATA(m_Int, true);
			LOG_SHARED_DATA(m_Float, true);
			LOG_SHARED_DATA(m_String, true);
			LOG_SHARED_DATA(m_Bool, true);
			LOG_SHARED_DATA(m_Ulong, true);
		};

		m_Int->SetCastedValue(pAgent->GetSharedData(), &Utility::VecIntEmpty);
		m_Float->SetCastedValue(pAgent->GetSharedData(), &Utility::VecFloatEmpty);
		m_Bool->SetCastedValue(pAgent->GetSharedData(), &Utility::VecBoolEmpty);
		m_Ulong->SetCastedValue(pAgent->GetSharedData(), &Utility::VecUlongEmpty);
		m_String->SetCastedValue(pAgent->GetSharedData(), &Utility::VecStringEmpty);

		pAgent->ProcessRegister();

		return NS_SUCCESS;
	}

}
