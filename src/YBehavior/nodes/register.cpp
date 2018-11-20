#include "YBehavior/nodes/register.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/registerdata.h"

namespace YBehavior
{
	bool ReadRegister::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeID;
		typeID = CreateVariable(m_Event, "Event", data, Utility::POINTER_CHAR);
		if (!m_Event)
		{
			return false;
		}

		typeID = CreateVariable(m_Int, "Int", data, Utility::POINTER_CHAR);
		if (!m_Int)
		{
			return false;
		}

		typeID = CreateVariable(m_Float, "Float", data, Utility::POINTER_CHAR);
		if (!m_Float)
		{
			return false;
		}

		typeID = CreateVariable(m_Bool, "Bool", data, Utility::POINTER_CHAR);
		if (!m_Bool)
		{
			return false;
		}

		typeID = CreateVariable(m_Ulong, "Ulong", data, Utility::POINTER_CHAR);
		if (!m_Ulong)
		{
			return false;
		}

		typeID = CreateVariable(m_String, "String", data, Utility::POINTER_CHAR);
		if (!m_String)
		{
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
		m_Event->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pEvent);
		m_Int->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecInt);
		m_Float->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecFloat);
		m_Bool->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecBool);
		m_Ulong->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecUlong);
		m_String->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecString);

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
		typeID = CreateVariable(m_Event, "Event", data);
		if (!m_Event)
		{
			return false;
		}

		typeID = CreateVariable(m_Int, "Int", data);
		if (!m_Int)
		{
			return false;
		}

		typeID = CreateVariable(m_Float, "Float", data);
		if (!m_Float)
		{
			return false;
		}

		typeID = CreateVariable(m_Bool, "Bool", data);
		if (!m_Bool)
		{
			return false;
		}

		typeID = CreateVariable(m_Ulong, "Ulong", data);
		if (!m_Ulong)
		{
			return false;
		}

		typeID = CreateVariable(m_String, "String", data);
		if (!m_String)
		{
			return false;
		}

		return true;
	}

	NodeState WriteRegister::Update(AgentPtr pAgent)
	{
		RegisterData* pRegister = pAgent->GetRegister();

		pRegister->GetSendData().pEvent = m_Event->GetCastedValue(pAgent->GetMemory());
		pRegister->GetSendData().pVecInt = m_Int->GetCastedValue(pAgent->GetMemory());
		pRegister->GetSendData().pVecFloat = m_Float->GetCastedValue(pAgent->GetMemory());
		pRegister->GetSendData().pVecBool = m_Bool->GetCastedValue(pAgent->GetMemory());
		pRegister->GetSendData().pVecUlong = m_Ulong->GetCastedValue(pAgent->GetMemory());
		pRegister->GetSendData().pVecString = m_String->GetCastedValue(pAgent->GetMemory());

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Event, true);
			LOG_SHARED_DATA(m_Int, true);
			LOG_SHARED_DATA(m_Float, true);
			LOG_SHARED_DATA(m_String, true);
			LOG_SHARED_DATA(m_Bool, true);
			LOG_SHARED_DATA(m_Ulong, true);
		};

		m_Int->SetCastedValue(pAgent->GetMemory(), &Utility::VecIntEmpty);
		m_Float->SetCastedValue(pAgent->GetMemory(), &Utility::VecFloatEmpty);
		m_Bool->SetCastedValue(pAgent->GetMemory(), &Utility::VecBoolEmpty);
		m_Ulong->SetCastedValue(pAgent->GetMemory(), &Utility::VecUlongEmpty);
		m_String->SetCastedValue(pAgent->GetMemory(), &Utility::VecStringEmpty);

		pAgent->ProcessRegister();

		return NS_SUCCESS;
	}

}
