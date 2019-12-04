#include "YBehavior/nodes/dice.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidVecTypes = {
		GetTypeID<VecInt>(),
		GetTypeID<VecFloat>(),
		GetTypeID<VecUint64>(),
	};

	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Uint64>(),
	};

	NodeState Dice::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_FAILURE;

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_Distribution, true);
			LOG_SHARED_DATA(m_Values, true);
			LOG_SHARED_DATA(m_Input, true);
			LOG_SHARED_DATA(m_Output, true);
			LOG_SHARED_DATA(m_IgnoreInput, true);
		}

		INT sizeX = m_Distribution->VectorSize(pAgent->GetMemory());
		INT sizeY = m_Values->VectorSize(pAgent->GetMemory());
		if (sizeX != sizeY)
		{
			DEBUG_LOG_INFO("Different length of X and Y; ");
			return NS_FAILURE;
		}
		if (sizeX == 0)
		{
			DEBUG_LOG_INFO("No key points; ");
			return NS_FAILURE;
		}

		IVariableOperationHelper* pHelper = m_Input->GetOperation();

		const void* x0 = m_Distribution->GetElement(pAgent->GetMemory(), 0);
		auto zero = pHelper->AllocTempData();

		const void* input;
		auto res = pHelper->AllocTempData();

		BOOL bIgnoreInput = Utility::FALSE_VALUE;
		m_IgnoreInput->GetCastedValue(pAgent->GetMemory(), bIgnoreInput);
		if (bIgnoreInput)
		{
			auto sum = pHelper->AllocTempData();

			for (INT i = 0; i < sizeX; ++i)
			{
				const void* x1 = m_Distribution->GetElement(pAgent->GetMemory(), i);
				pHelper->Calculate(sum.pData, sum.pData, x1, OT_ADD);
			}
			pHelper->Random(res.pData, zero.pData, sum.pData);
			input = res.pData;
		}
		else
		{
			input = m_Input->GetValue(pAgent->GetMemory());
		}

		if (pHelper->Compare(input, zero.pData, OT_LESS))
		{
			DEBUG_LOG_INFO("Input below zero; ");
			return NS_FAILURE;
		}

		auto current = pHelper->AllocTempData();

		for (INT i = 0; i < sizeX; ++i)
		{
			const void* x0 = m_Distribution->GetElement(pAgent->GetMemory(), i);
			pHelper->Calculate(current.pData, current.pData, x0, OT_ADD);

			///> in the range of this (x0, x1)
			if (pHelper->Compare(input, current.pData, OT_LESS))
			{
				ns = NS_SUCCESS;
				m_Output->SetValue(pAgent->GetMemory(), m_Values->GetElement(pAgent->GetMemory(), i));
				break;
			}
		}

		if (ns == NS_FAILURE)
		{
			DEBUG_LOG_INFO("Input above max; ");
		}
		//if (m_DefaultChild != nullptr)
		//{
		//	DEBUG_LOG_INFO("Switch to default; ");
		//	ns = m_DefaultChild->Execute(pAgent);
		//}

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Output, false);

		return ns;
	}

	bool Dice::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID xVecType = CreateVariable(m_Distribution, "Distribution", data);
		if (s_ValidVecTypes.find(xVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Distribution in Dice: " << xVecType << ERROR_END;
			return false;
		}
		TYPEID yVecType = CreateVariable(m_Values, "Values", data);
		if (s_ValidVecTypes.find(yVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Values in Dice: " << yVecType << ERROR_END;
			return false;
		}

		TYPEID xType = CreateVariable(m_Input, "Input", data);
		if (s_ValidTypes.find(xType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Input in Dice: " << xType << ERROR_END;
			return false;
		}
		TYPEID yType = CreateVariable(m_Output, "Output", data);
		if (s_ValidTypes.find(yType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for Output in Dice: " << yType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(xType, xVecType))
		{
			ERROR_BEGIN << "Different types in Dice:  " << xType << " and " << xVecType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(yType, yVecType))
		{
			ERROR_BEGIN << "Different types in Dice:  " << yType << " and " << yVecType << ERROR_END;
			return false;
		}

		CreateVariable(m_IgnoreInput, "IgnoreInput", data);
		if (!m_IgnoreInput)
		{
			return false;
		}

		return true;
	}
}