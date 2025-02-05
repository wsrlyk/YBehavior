#include "dice.h"
#include "YBehavior/agent.h"
#include "YBehavior/pincreation.h"
#include "../operations/datacompare.h"
#include "../operations/datacalculate.h"
#include "../operations/dataoperation.h"
#include "../operations/datarandom.h"
#include <set>
namespace YBehavior
{
	static std::set<TYPEID> s_ValidVecTypes = {
		GetTypeID<VecInt>(),
		GetTypeID<VecFloat>(),
		GetTypeID<VecUint64>(),
	};

	static std::set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Uint64>(),
	};

	NodeState Dice::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_FAILURE;

		INT sizeX = m_Distribution->ArraySize(pAgent->GetMemory());
		INT sizeY = m_Values->ArraySize(pAgent->GetMemory());
		if (sizeX != sizeY)
		{
			YB_LOG_INFO("Different length of Distribution and Values; ");
			return NS_FAILURE;
		}
		if (sizeX == 0)
		{
			YB_LOG_INFO("No Distribution; ");
			return NS_FAILURE;
		}

		auto zero = m_pOperationHelper->AllocTempData();

		const void* input;
		auto res = m_pOperationHelper->AllocTempData();

		if (!m_Input)
		{
			auto sum = m_pOperationHelper->AllocTempData();

			for (INT i = 0; i < sizeX; ++i)
			{
				const void* x1 = m_Distribution->GetElementPtr(pAgent->GetMemory(), i);
				m_pCalculateHelper->Calculate(sum.pData, sum.pData, x1, CalculateType::ADD);
			}
			m_pRandomHelper->Random(res.pData, zero.pData, sum.pData);
			input = res.pData;
		}
		else
		{
			input = m_Input->GetValuePtr(pAgent->GetMemory());
		}

		if (m_pCompareHelper->Compare(input, zero.pData, CompareType::LESS))
		{
			YB_LOG_INFO("Input below zero; ");
			return NS_FAILURE;
		}

		auto current = m_pOperationHelper->AllocTempData();

		for (INT i = 0; i < sizeX; ++i)
		{
			const void* x0 = m_Distribution->GetElementPtr(pAgent->GetMemory(), i);
			m_pCalculateHelper->Calculate(current.pData, current.pData, x0, CalculateType::ADD);

			///> in the range of this (x0, x1)
			if (m_pCompareHelper->Compare(input, current.pData, CompareType::LESS))
			{
				ns = NS_SUCCESS;
				m_Output->SetValue(pAgent->GetMemory(), m_Values->GetElementPtr(pAgent->GetMemory(), i));
				break;
			}
		}

		if (ns == NS_FAILURE)
		{
			YB_LOG_INFO("Input above max; ");
		}
		//if (m_DefaultChild != nullptr)
		//{
		//	YB_LOG_INFO("Switch to default; ");
		//	ns = m_DefaultChild->Execute(pAgent);
		//}

		return ns;
	}

	bool Dice::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID xVecType = PinCreation::CreatePin(this, m_Distribution, "Distribution", data);
		if (s_ValidVecTypes.find(xVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Distribution in Dice: " << xVecType << ERROR_END;
			return false;
		}
		TYPEID yVecType = PinCreation::CreatePin(this, m_Values, "Values", data);
		if (!m_Values)
		{
			return false;
		}
		//if (s_ValidVecTypes.find(yVecType) == s_ValidVecTypes.end())
		//{
		//	ERROR_BEGIN_NODE_HEAD << "Invalid type for Values in Dice: " << yVecType << ERROR_END;
		//	return false;
		//}

		TYPEID xType = PinCreation::CreatePinIfExist(this, m_Input, "Input", data);
		if (m_Input && s_ValidTypes.find(xType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Input in Dice: " << xType << ERROR_END;
			return false;
		}
		TYPEID yType = PinCreation::CreatePin(this, m_Output, "Output", data, PinCreation::Flag::IsOutput);
		if (!m_Output)
		{
			return false;
		}
		//if (s_ValidTypes.find(yType) == s_ValidTypes.end())
		//{
		//	ERROR_BEGIN_NODE_HEAD << "Invalid type for Output in Dice: " << yType << ERROR_END;
		//	return false;
		//}

		if (m_Input && !Utility::IsElement(xType, xVecType))
		{
			ERROR_BEGIN_NODE_HEAD << "Different types in Dice:  " << xType << " and " << xVecType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(yType, yVecType))
		{
			ERROR_BEGIN_NODE_HEAD << "Different types in Dice:  " << yType << " and " << yVecType << ERROR_END;
			return false;
		}

		auto helperType = m_Distribution->ElementTypeID();
		m_pCalculateHelper = DataCalculateMgr::Instance()->Get(helperType);
		m_pCompareHelper = DataCompareMgr::Instance()->Get(helperType);
		m_pOperationHelper = DataOperationMgr::Instance()->Get(helperType);
		m_pRandomHelper = DataRandomMgr::Instance()->Get(helperType);
		if (!m_pCalculateHelper || !m_pCompareHelper || !m_pOperationHelper || !m_pRandomHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "This type is not supported by Dice." << ERROR_END;
			return false;
		}
		return true;
	}
}