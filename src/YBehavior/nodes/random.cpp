#include "YBehavior/nodes/random.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/variablecreation.h"
#include "YBehavior/variables/variablerandom.h"
#include "YBehavior/variables/variableoperation.h"
#include <set>

namespace YBehavior
{
	static std::set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Bool>(),
	};

	bool Random::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		///> Left
		auto dataTypeL = VariableCreation::CreateVariable(this, m_Target, "Target", data, true);
		if (s_ValidTypes.find(dataTypeL) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Target in Random: " << dataTypeL << ERROR_END;
			return false;
		}
		///> Right1
		auto dataTypeR = VariableCreation::CreateVariable(this, m_Bound1, "Bound1", data);
		if (dataTypeL != dataTypeR)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  " << dataTypeL << " and " << dataTypeR << ERROR_END;
			return false;
		}
		///> Right2
		dataTypeR = VariableCreation::CreateVariable(this, m_Bound2, "Bound2", data);
		if (dataTypeL != dataTypeR)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  " << dataTypeL << " and " << dataTypeR << ERROR_END;
			return false;
		}

		m_pHelper = VariableRandomMgr::Instance()->Get(dataTypeL);
		if (!m_pHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "This type is not supported by RandomNode." << ERROR_END;
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

		m_pHelper->Random(pAgent->GetMemory(), m_Target, m_Bound1, m_Bound2);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Target, false);
		}

		return NS_SUCCESS;
	}

	bool RandomSelect::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDArray = VariableCreation::CreateVariable(this, m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeID = VariableCreation::CreateVariable(this, m_Output, "Output", data);
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
		TYPEID typeIDInput = VariableCreation::CreateVariable(this, m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeIDOutput = VariableCreation::CreateVariable(this, m_Output, "Output", data, true);
		if (typeIDOutput != typeIDInput)
		{
			ERROR_BEGIN_NODE_HEAD << "Permulation types not match " << typeIDOutput << " and " << typeIDInput << ERROR_END;
			return false;
		}

		m_bSameArray = m_Input->GetKey() == m_Output->GetKey();

		m_pHelper = VariableOperationMgr::Instance()->Get(m_Input->ElementTypeID());
		if (!m_pHelper)
			return false;
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

		//if (m_bSameArray)
		auto temp = m_pHelper->AllocTempData();

		for (int i = length - 1; i > 0; --i)
		{
			int j = Utility::Rand(0, i + 1);

			if (j == i)
			{
				continue;
			}

			m_pHelper->Set(temp.pData, pTargetVariable->GetElement(pAgent->GetMemory(), j));
			m_pHelper->Set(const_cast<void*>(pTargetVariable->GetElement(pAgent->GetMemory(), j)), pTargetVariable->GetElement(pAgent->GetMemory(), i));
			m_pHelper->Set(const_cast<void*>(pTargetVariable->GetElement(pAgent->GetMemory(), i)), temp.pData);
		}

		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Output, false);

		return NS_SUCCESS;
	}

}
