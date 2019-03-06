#include "YBehavior/nodes/array.h"
#include "YBehavior/agent.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{

	bool GetArrayLength::OnLoaded(const pugi::xml_node& data)
	{
		CreateVariable(m_Array, "Array", data);
		if (m_Array == nullptr)
		{
			return false;
		}

		TYPEID typeID = CreateVariable(m_Length, "Length", data, Utility::POINTER_CHAR);
		if (!m_Length)
		{
			return false;
		}
		return true;
	}

	YBehavior::NodeState GetArrayLength::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Array, true);
		}
		INT size = m_Array->VectorSize(pAgent->GetMemory());
		m_Length->SetCastedValue(pAgent->GetMemory(), &size);

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Length, false);
		}

		return NS_SUCCESS;
	}

	bool ClearArray::OnLoaded(const pugi::xml_node& data)
	{
		CreateVariable(m_Array, "Array", data);
		if (m_Array == nullptr)
		{
			return false;
		}
		return true;
	}

	YBehavior::NodeState ClearArray::Update(AgentPtr pAgent)
	{
		m_Array->Clear(pAgent->GetMemory());
		return NS_SUCCESS;
	}

	bool ArrayPushElement::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDArray = CreateVariable(m_Array, "Array", data);
		if (m_Array == nullptr)
		{
			return false;
		}

		TYPEID typeID = CreateVariable(m_Element, "Element", data);
		if (!Utility::IsElement(typeID, typeIDArray))
		{
			ERROR_BEGIN << "ArrayPushElement types not match " << typeID << " and " << typeIDArray << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState ArrayPushElement::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Element, true);
		}
		m_Array->PushBackElement(pAgent->GetMemory(), m_Element->GetValue(pAgent->GetMemory()));

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Array, false);
		}

		return NS_SUCCESS;
	}

	bool IsArrayEmpty::OnLoaded(const pugi::xml_node& data)
	{
		CreateVariable(m_Array, "Array", data);
		if (m_Array == nullptr)
		{
			return false;
		}
		return true;
	}

	YBehavior::NodeState IsArrayEmpty::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Array, true);
		}
		INT size = m_Array->VectorSize(pAgent->GetMemory());
		if (size != 0)
			return NS_FAILURE;
		return NS_SUCCESS;
	}
}