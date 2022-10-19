#include "YBehavior/nodes/dice.h"
#include "YBehavior/agent.h"
#include "YBehavior/variablecreation.h"
#include "YBehavior/variables/variablecompare.h"
#include "YBehavior/variables/variablecalculate.h"
#include "YBehavior/variables/variableoperation.h"
#include "YBehavior/variables/variablerandom.h"
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

		YB_IF_HAS_DEBUG_POINT
		{
			YB_LOG_VARIABLE(m_Distribution, true);
			YB_LOG_VARIABLE(m_Values, true);
			YB_LOG_VARIABLE(m_Input, true);
			YB_LOG_VARIABLE(m_Output, true);
		}

		INT sizeX = m_Distribution->VectorSize(pAgent->GetMemory());
		INT sizeY = m_Values->VectorSize(pAgent->GetMemory());
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
				const void* x1 = m_Distribution->GetElement(pAgent->GetMemory(), i);
				m_pCalculateHelper->Calculate(sum.pData, sum.pData, x1, CalculateType::ADD);
			}
			m_pRandomHelper->Random(res.pData, zero.pData, sum.pData);
			input = res.pData;
		}
		else
		{
			input = m_Input->GetValue(pAgent->GetMemory());
		}

		if (m_pCompareHelper->Compare(input, zero.pData, CompareType::LESS))
		{
			YB_LOG_INFO("Input below zero; ");
			return NS_FAILURE;
		}

		auto current = m_pOperationHelper->AllocTempData();

		for (INT i = 0; i < sizeX; ++i)
		{
			const void* x0 = m_Distribution->GetElement(pAgent->GetMemory(), i);
			m_pCalculateHelper->Calculate(current.pData, current.pData, x0, CalculateType::ADD);

			///> in the range of this (x0, x1)
			if (m_pCompareHelper->Compare(input, current.pData, CompareType::LESS))
			{
				ns = NS_SUCCESS;
				m_Output->SetValue(pAgent->GetMemory(), m_Values->GetElement(pAgent->GetMemory(), i));
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

		YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Output, false);

		return ns;
	}

	bool Dice::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID xVecType = VariableCreation::CreateVariable(this, m_Distribution, "Distribution", data);
		if (s_ValidVecTypes.find(xVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Distribution in Dice: " << xVecType << ERROR_END;
			return false;
		}
		TYPEID yVecType = VariableCreation::CreateVariable(this, m_Values, "Values", data);
		if (!m_Values)
		{
			return false;
		}
		//if (s_ValidVecTypes.find(yVecType) == s_ValidVecTypes.end())
		//{
		//	ERROR_BEGIN_NODE_HEAD << "Invalid type for Values in Dice: " << yVecType << ERROR_END;
		//	return false;
		//}

		TYPEID xType = VariableCreation::CreateVariableIfExist(this, m_Input, "Input", data);
		if (m_Input && s_ValidTypes.find(xType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Input in Dice: " << xType << ERROR_END;
			return false;
		}
		TYPEID yType = VariableCreation::CreateVariable(this, m_Output, "Output", data);
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
		m_pCalculateHelper = VariableCalculateMgr::Instance()->Get(helperType);
		m_pCompareHelper = VariableCompareMgr::Instance()->Get(helperType);
		m_pOperationHelper = VariableOperationMgr::Instance()->Get(helperType);
		m_pRandomHelper = VariableRandomMgr::Instance()->Get(helperType);
		if (!m_pCalculateHelper || !m_pCompareHelper || !m_pOperationHelper || !m_pRandomHelper)
		{
			ERROR_BEGIN_NODE_HEAD << "This type is not supported by Dice." << ERROR_END;
			return false;
		}
		return true;
	}
}