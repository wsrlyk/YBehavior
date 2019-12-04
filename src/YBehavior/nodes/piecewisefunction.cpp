#include "YBehavior/nodes/piecewisefunction.h"
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

	NodeState PiecewiseFunction::Update(AgentPtr pAgent)
	{
		NodeState ns = NS_FAILURE;

		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_KeyPointX, true);
			LOG_SHARED_DATA(m_KeyPointY, true);
			LOG_SHARED_DATA(m_InputX, true);
			LOG_SHARED_DATA(m_OutputY, true);
		}

		INT sizeX = m_KeyPointX->VectorSize(pAgent->GetMemory());
		INT sizeY = m_KeyPointY->VectorSize(pAgent->GetMemory());
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
		else if (sizeX == 1)
		{
			DEBUG_LOG_INFO("Only one key point, return it; ");
			m_OutputY->SetValue(pAgent->GetMemory(), m_KeyPointY->GetValue(pAgent->GetMemory()));
			return NS_SUCCESS;
		}

		IVariableOperationHelper* pHelper = m_InputX->GetOperation();

		///>   y = y0 + (y1 - y0) * (x - x0) / (x1 - x0)
		///>   y = y0 + pDeltaY * pOffsetX / pDeltaX
		///>   y = y0 + pDeltaY * pRatio  (Here we wont calc pRatio first, in case that offsetX and DeltaX are intergers, leading to inaccurate division.
		///>								Instead, we calc pDeltaY * pOffsetX first
		///>   y = y0 + pOffsetY

		auto deltaX = pHelper->AllocTempData();
		auto deltaY = pHelper->AllocTempData();
		auto offsetX = pHelper->AllocTempData();
		auto deltaYxpOffsetX = pHelper->AllocTempData();
		auto offsetY = pHelper->AllocTempData();

		const void* x = m_InputX->GetValue(pAgent->GetMemory());
		for (INT i = 0; i < sizeX - 1; ++i)
		{
			const void* x0 = m_KeyPointX->GetElement(pAgent->GetMemory(), i);
			const void* x1 = m_KeyPointX->GetElement(pAgent->GetMemory(), i + 1);

			///> Not in the range of this (x0, x1]
			if (pHelper->Compare(x, x1, OT_GREATER) && i < sizeX - 2)
				continue;
			const void* y0 = m_KeyPointY->GetElement(pAgent->GetMemory(), i);
			const void* y1 = m_KeyPointY->GetElement(pAgent->GetMemory(), i + 1);

			if (x0 == nullptr || x1 == nullptr || y0 == nullptr || y1 == nullptr)
			{
				DEBUG_LOG_INFO("Null value at keypoint index " << i <<"; ");
				continue;
			}

			pHelper->Calculate(deltaX.pData, x1, x0, OT_SUB);
			pHelper->Calculate(offsetX.pData, x, x0, OT_SUB);
			pHelper->Calculate(deltaY.pData, y1, y0, OT_SUB);
			pHelper->Calculate(deltaYxpOffsetX.pData, deltaY.pData, offsetX.pData, OT_MUL);
			pHelper->Calculate(offsetY.pData, deltaYxpOffsetX.pData, deltaX.pData, OT_DIV);
			const void* res = pHelper->Calculate(y0, offsetY.pData, OT_ADD);

			m_OutputY->SetValue(pAgent->GetMemory(), res);
			ns = NS_SUCCESS;
			break;
			//if (onecase == nullptr)
			//	continue;

			//if (pHelper->Compare(pAgent->GetSharedData(), m_Switch->GetValue(pAgent->GetSharedData()), onecase, OT_EQUAL))
			//{
			//	DEBUG_LOG_INFO("Switch to case " << Utility::ToString(m_CasesChilds[i]->GetUID()) << "; ");

			//	ns = m_CasesChilds[i]->Execute(pAgent);
			//	return ns;
			//}
		}

		//if (m_DefaultChild != nullptr)
		//{
		//	DEBUG_LOG_INFO("Switch to default; ");
		//	ns = m_DefaultChild->Execute(pAgent);
		//}

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_OutputY, false);

		return ns;
	}

	bool PiecewiseFunction::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID xVecType = CreateVariable(m_KeyPointX, "KeyPointX", data);
		if (s_ValidVecTypes.find(xVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for KeyPointX in PiecewiseFunction: " << xVecType << ERROR_END;
			return false;
		}
		TYPEID yVecType = CreateVariable(m_KeyPointY, "KeyPointY", data);
		if (s_ValidVecTypes.find(yVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for KeyPointY in PiecewiseFunction: " << yVecType << ERROR_END;
			return false;
		}

		TYPEID xType = CreateVariable(m_InputX, "InputX", data);
		if (s_ValidTypes.find(xType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for InputX in PiecewiseFunction: " << xType << ERROR_END;
			return false;
		}
		TYPEID yType = CreateVariable(m_OutputY, "OutputY", data);
		if (s_ValidTypes.find(yType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for OutputY in PiecewiseFunction: " << yType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(xType, xVecType))
		{
			ERROR_BEGIN << "Different types in PiecewiseFunction:  " << xType << " and " << xVecType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(yType, yVecType))
		{
			ERROR_BEGIN << "Different types in PiecewiseFunction:  " << yType << " and " << yVecType << ERROR_END;
			return false;
		}

		return true;
	}
}