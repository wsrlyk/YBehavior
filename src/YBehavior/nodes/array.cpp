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

		CreateVariable(m_Length, "Length", data, Utility::POINTER_CHAR);
		if (!m_Length)
		{
			return false;
		}
		return true;
	}

	YBehavior::NodeState GetArrayLength::Update(AgentPtr pAgent)
	{
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Array, true);
		}
		INT size = m_Array->VectorSize(pAgent->GetMemory());
		m_Length->SetCastedValue(pAgent->GetMemory(), &size);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Length, false);
		}

		return NS_SUCCESS;
	}

	bool ClearArray::OnLoaded(const pugi::xml_node& data)
	{
		CreateVariable(m_Array, "Array", data, Utility::POINTER_CHAR);
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
		TYPEID typeIDArray = CreateVariable(m_Array, "Array", data, Utility::POINTER_CHAR);
		if (m_Array == nullptr)
		{
			return false;
		}

		TYPEID typeID = CreateVariable(m_Element, "Element", data);
		if (!Utility::IsElement(typeID, typeIDArray))
		{
			ERROR_BEGIN_NODE_HEAD << "types not match " << typeID << " and " << typeIDArray << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState ArrayPushElement::Update(AgentPtr pAgent)
	{
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Element, true);
		}
		m_Array->PushBackElement(pAgent->GetMemory(), m_Element->GetValue(pAgent->GetMemory()));

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Array, false);
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
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Array, true);
		}
		INT size = m_Array->VectorSize(pAgent->GetMemory());
		if (size != 0)
			return NS_FAILURE;
		return NS_SUCCESS;
	}

	bool GenIndexArray::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID inputType = CreateVariable(m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}
		CreateVariable(m_Output, "Output", data, Utility::POINTER_CHAR);
		if (m_Output == nullptr)
		{
			return false;
		}
		if (!m_Input->IsThisVector() && inputType != GetTypeID<INT>())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type of Input " << inputType << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState GenIndexArray::Update(AgentPtr pAgent)
	{
		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Input, true);
		INT size = 0;
		if (m_Input->IsThisVector())
		{
			size = m_Input->VectorSize(pAgent->GetMemory());
		}
		else
		{
			auto v = m_Input->GetValue(pAgent->GetMemory());
			if (v)
			{
				size = *((INT*)v);
			}
			else
			{
				YB_LOG_INFO("Cant get value from Input; ");
				return NS_FAILURE;
			}
		}

		std::vector<INT> o;
		for (INT i = 0; i < size; ++i)
		{
			o.push_back(i);
		}
		m_Output->SetCastedValue(pAgent->GetMemory(), o);
		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Output, false);
		return NS_SUCCESS;
	}

}