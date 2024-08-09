#include "YBehavior/nodes/random.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/pin.h"
#include "YBehavior/pincreation.h"
#include "YBehavior/operations/datarandom.h"
#include "YBehavior/operations/dataoperation.h"
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
		auto dataTypeL = PinCreation::CreatePin(this, m_Target, "Target", data, true);
		if (s_ValidTypes.find(dataTypeL) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Target in Random: " << dataTypeL << ERROR_END;
			return false;
		}
		///> Right1
		auto dataTypeR = PinCreation::CreatePin(this, m_Bound1, "Bound1", data);
		if (dataTypeL != dataTypeR)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  " << dataTypeL << " and " << dataTypeR << ERROR_END;
			return false;
		}
		///> Right2
		dataTypeR = PinCreation::CreatePin(this, m_Bound2, "Bound2", data);
		if (dataTypeL != dataTypeR)
		{
			ERROR_BEGIN_NODE_HEAD << "Different types:  " << dataTypeL << " and " << dataTypeR << ERROR_END;
			return false;
		}

		m_pHelper = DataRandomMgr::Instance()->Get(dataTypeL);
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
			YB_LOG_PIN(m_Bound1, true);
			YB_LOG_PIN(m_Bound2, true);
			YB_LOG_PIN(m_Target, true);
		}

		m_pHelper->Random(pAgent->GetMemory(), m_Target, m_Bound1, m_Bound2);

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_PIN(m_Target, false);
		}

		return NS_SUCCESS;
	}

	bool RandomSelect::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDArray = PinCreation::CreatePin(this, m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeID = PinCreation::CreatePin(this, m_Output, "Output", data);
		if (!Utility::IsElement(typeID, typeIDArray))
		{
			ERROR_BEGIN_NODE_HEAD << "RandomSelect types not match " << typeID << " and " << typeIDArray << ERROR_END;
			return false;
		}
		return true;
	}

	YBehavior::NodeState RandomSelect::Update(AgentPtr pAgent)
	{
		YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Input, true);

		INT size = m_Input->ArraySize(pAgent->GetMemory());
		if (size == 0)
		{
			return NS_FAILURE;
		}
		INT idx = Utility::Rand(0, size);

		m_Output->SetValue(pAgent->GetMemory(), m_Input->GetElementPtr(pAgent->GetMemory(), idx));

		YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Output, false);
		return NS_SUCCESS;
	}

	bool Shuffle::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID typeIDInput = PinCreation::CreatePin(this, m_Input, "Input", data);
		if (m_Input == nullptr)
		{
			return false;
		}

		TYPEID typeIDOutput = PinCreation::CreatePin(this, m_Output, "Output", data, true);
		if (typeIDOutput != typeIDInput)
		{
			ERROR_BEGIN_NODE_HEAD << "Permulation types not match " << typeIDOutput << " and " << typeIDInput << ERROR_END;
			return false;
		}

		m_bSameArray = m_Input->GetKey() == m_Output->GetKey();

		m_pHelper = DataOperationMgr::Instance()->Get(m_Input->ElementTypeID());
		if (!m_pHelper)
			return false;
		return true;
	}

	YBehavior::NodeState Shuffle::Update(AgentPtr pAgent)
	{
		YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Input, true);

		int length = m_Input->ArraySize(pAgent->GetMemory());
		if (length == 0)
			return NS_SUCCESS;

		IPin* pTargetVariable = nullptr;
		if (!m_bSameArray)
		{
			m_Output->Clear(pAgent->GetMemory());
			for (int i = 0; i < length; ++i)
			{
				m_Output->PushBackElement(pAgent->GetMemory(), m_Input->GetElementPtr(pAgent->GetMemory(), i));
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

			m_pHelper->Set(temp.pData, pTargetVariable->GetElementPtr(pAgent->GetMemory(), j));
			m_pHelper->Set(const_cast<void*>(pTargetVariable->GetElementPtr(pAgent->GetMemory(), j)), pTargetVariable->GetElementPtr(pAgent->GetMemory(), i));
			m_pHelper->Set(const_cast<void*>(pTargetVariable->GetElementPtr(pAgent->GetMemory(), i)), temp.pData);
		}

		YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Output, false);

		return NS_SUCCESS;
	}

}
