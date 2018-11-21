#include "YBehavior/nodes/random.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Bool>(),
	};

	bool Random::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		m_DataType = CreateVariable(m_Opl, "Target", data, Utility::POINTER_CHAR);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Opl in Comparer: " << m_DataType << ERROR_END;
			return false;
		}
		///> Right1
		TYPEID dataType = CreateVariable(m_Opr1, "Bound1", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return false;
		}
		///> Right2
		dataType = CreateVariable(m_Opr2, "Bound2", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return false;
		}

		return true;
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
		pHelper->Random(pAgent->GetMemory(), m_Opl, m_Opr1, m_Opr2);

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Opl, false);
		}

		return NS_SUCCESS;
	}

	bool RandomSelect::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDArray = CreateVariable(m_Input, "Input", data, ST_ARRAY);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeID = CreateVariable(m_Output, "Output", data, ST_SINGLE);
		if (!Utility::IsElement(typeID, typeIDArray))
		{
			ERROR_BEGIN << "RandomSelect types not match " << typeID << " and " << typeIDArray << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState RandomSelect::Update(AgentPtr pAgent)
	{
		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Input, true);

		INT size = m_Input->VectorSize(pAgent->GetMemory());
		if (size == 0)
		{
			return NS_FAILURE;
		}
		INT idx = Utility::Rand(0, size);

		m_Output->SetValue(pAgent->GetMemory(), m_Input->GetElement(pAgent->GetMemory(), idx));

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Output, false);
		return NS_SUCCESS;
	}

	bool MessUp::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDInput = CreateVariable(m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeIDOutput = CreateVariable(m_Output, "Output", data, Utility::POINTER_CHAR);
		if (typeIDOutput != typeIDInput)
		{
			ERROR_BEGIN << "Permulation types not match " << typeIDOutput << " and " << typeIDInput << ERROR_END;
			return false;
		}

		m_bSameArray = m_Input->GetKey() == m_Output->GetKey();
		return true;
	}

	YBehavior::NodeState MessUp::Update(AgentPtr pAgent)
	{
		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Input, true);

		int length = m_Input->VectorSize(pAgent->GetMemory());
		if (length == 0)
			return NS_SUCCESS;

		ISharedVariableEx* pTargetVariable = nullptr;
		if (!m_bSameArray)
		{
			m_Output->Clear(pAgent->GetMemory());
			for (int i = 0; i < length - 1; ++i)
			{
				m_Output->PushBackElement(pAgent->GetMemory(), m_Input->GetElement(pAgent->GetMemory(), i));
			}
			pTargetVariable = m_Output;
		}
		else
		{
			pTargetVariable = m_Input;
		}

		void* pTemp = nullptr;
		IVariableOperationHelper* pHelper = m_Input->GetElementOperation();
		if (m_bSameArray)
			pTemp = pHelper->AllocData();

		for (int i = length - 1; i > 0; --i)
		{
			int j = Utility::Rand(0, i + 1);

			if (j == i)
			{
				continue;
			}

			pHelper->Set(pTemp, pTargetVariable->GetElement(pAgent->GetMemory(), j));
			pHelper->Set(const_cast<void*>(pTargetVariable->GetElement(pAgent->GetMemory(), j)), pTargetVariable->GetElement(pAgent->GetMemory(), i));
			pHelper->Set(const_cast<void*>(pTargetVariable->GetElement(pAgent->GetMemory(), i)), pTemp);
		}

		if (!pTemp)
			pHelper->RecycleData(pTemp);

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Output, false);

		return NS_SUCCESS;
	}

}
