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
		m_DataType = CreateVariable(m_Target, "Target", data, true);
		if (s_ValidTypes.find(m_DataType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Target in Comparer: " << m_DataType << ERROR_END;
			return false;
		}
		///> Right1
		TYPEID dataType = CreateVariable(m_Bound1, "Bound1", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return false;
		}
		///> Right2
		dataType = CreateVariable(m_Bound2, "Bound2", data);
		if (m_DataType != dataType)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  " << dataType << " and " << m_DataType << ERROR_END;
			return false;
		}

		return true;
	}

	YBehavior::NodeState Random::Update(AgentPtr pAgent)
	{
		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Bound1, true);
			YB_LOG_VARIABLE(m_Bound2, true);
			YB_LOG_VARIABLE(m_Target, true);
		}

		IVariableOperationHelper* pHelper = m_Target->GetOperation();
		pHelper->Random(pAgent->GetMemory(), m_Target, m_Bound1, m_Bound2);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Target, false);
		}

		return NS_SUCCESS;
	}

	bool RandomSelect::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDArray = CreateVariable(m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeID = CreateVariable(m_Output, "Output", data);
		if (!Utility::IsElement(typeID, typeIDArray))
		{
			ERROR_BEGIN_NODE_HEAD << "RandomSelect types not match " << typeID << " and " << typeIDArray << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState RandomSelect::Update(AgentPtr pAgent)
	{
		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Input, true);

		INT size = m_Input->VectorSize(pAgent->GetMemory());
		if (size == 0)
		{
			return NS_FAILURE;
		}
		INT idx = Utility::Rand(0, size);

		m_Output->SetValue(pAgent->GetMemory(), m_Input->GetElement(pAgent->GetMemory(), idx));

		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Output, false);
		return NS_SUCCESS;
	}

	bool Shuffle::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDInput = CreateVariable(m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeIDOutput = CreateVariable(m_Output, "Output", data, true);
		if (typeIDOutput != typeIDInput)
		{
			ERROR_BEGIN_NODE_HEAD << "Permulation types not match " << typeIDOutput << " and " << typeIDInput << ERROR_END;
			return false;
		}

		m_bSameArray = m_Input->GetKey() == m_Output->GetKey();
		return true;
	}

	YBehavior::NodeState Shuffle::Update(AgentPtr pAgent)
	{
		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Input, true);

		int length = m_Input->VectorSize(pAgent->GetMemory());
		if (length == 0)
			return NS_SUCCESS;

		ISharedVariableEx* pTargetVariable = nullptr;
		if (!m_bSameArray)
		{
			m_Output->Clear(pAgent->GetMemory());
			for (int i = 0; i < length; ++i)
			{
				m_Output->PushBackElement(pAgent->GetMemory(), m_Input->GetElement(pAgent->GetMemory(), i));
			}
			pTargetVariable = m_Output;
		}
		else
		{
			pTargetVariable = m_Input;
		}

		IVariableOperationHelper* pHelper = m_Input->GetElementOperation();
		//if (m_bSameArray)
		auto temp = pHelper->AllocTempData();

		for (int i = length - 1; i > 0; --i)
		{
			int j = Utility::Rand(0, i + 1);

			if (j == i)
			{
				continue;
			}

			pHelper->Set(temp.pData, pTargetVariable->GetElement(pAgent->GetMemory(), j));
			pHelper->Set(const_cast<void*>(pTargetVariable->GetElement(pAgent->GetMemory(), j)), pTargetVariable->GetElement(pAgent->GetMemory(), i));
			pHelper->Set(const_cast<void*>(pTargetVariable->GetElement(pAgent->GetMemory(), i)), temp.pData);
		}

		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Output, false);

		return NS_SUCCESS;
	}

}
