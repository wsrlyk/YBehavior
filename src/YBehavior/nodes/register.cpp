#include "YBehavior/nodes/register.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/registerdata.h"
#include "YBehavior/variablecreation.h"

namespace YBehavior
{
	bool ReadRegister::OnLoaded(const pugi::xml_node& data)
	{
		VariableCreation::CreateVariable(this, m_Event, "Event", data, true);
		if (!m_Event)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Int, "Int", data, true);
		if (!m_Int)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Float, "Float", data, true);
		if (!m_Float)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Bool, "Bool", data, true);
		if (!m_Bool)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Ulong, "Ulong", data, true);
		if (!m_Ulong)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_String, "String", data, true);
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
			YB_LOG_INFO("No Event.");
			return NS_FAILURE;
		}
		m_Event->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pEvent);
		m_Int->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecInt);
		m_Float->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecFloat);
		m_Bool->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecBool);
		m_Ulong->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecUlong);
		m_String->SetValue(pAgent->GetMemory(), pRegister->GetReceiveData().pVecString);

		pRegister->Clear();

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Event, false);
			YB_LOG_VARIABLE(m_Int, false);
			YB_LOG_VARIABLE(m_Float, false);
			YB_LOG_VARIABLE(m_String, false);
			YB_LOG_VARIABLE(m_Bool, false);
			YB_LOG_VARIABLE(m_Ulong, false);
		}

		return NS_SUCCESS;
	}


	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


	bool WriteRegister::OnLoaded(const pugi::xml_node& data)
	{
		VariableCreation::CreateVariable(this, m_Event, "Event", data);
		if (!m_Event)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Int, "Int", data);
		if (!m_Int)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Float, "Float", data);
		if (!m_Float)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Bool, "Bool", data);
		if (!m_Bool)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_Ulong, "Ulong", data);
		if (!m_Ulong)
		{
			return false;
		}

		VariableCreation::CreateVariable(this, m_String, "String", data);
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

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Event, true);
			YB_LOG_VARIABLE(m_Int, true);
			YB_LOG_VARIABLE(m_Float, true);
			YB_LOG_VARIABLE(m_String, true);
			YB_LOG_VARIABLE(m_Bool, true);
			YB_LOG_VARIABLE(m_Ulong, true);
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
