#include "YBehavior/nodes/piecewisefunction.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#include "YBehavior/utility.h"
#endif // DEBUGGER
#include "YBehavior/agent.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidVecTypes = {
		GetClassTypeNumberId<VecInt>(),
		GetClassTypeNumberId<VecFloat>(),
		GetClassTypeNumberId<VecUint64>(),
	};

	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetClassTypeNumberId<Int>(),
		GetClassTypeNumberId<Float>(),
		GetClassTypeNumberId<Uint64>(),
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

		INT sizeX = m_KeyPointX->VectorSize(pAgent->GetSharedData());
		INT sizeY = m_KeyPointY->VectorSize(pAgent->GetSharedData());
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
			m_OutputY->SetValue(pAgent->GetSharedData(), m_KeyPointY->GetValue(pAgent->GetSharedData()));
			return NS_SUCCESS;
		}

		IVariableOperationHelper* pHelper = m_InputX->GetOperation();

		///>   y = y0 + (y1 - y0) * (x - x0) / (x1 - x0)
		///>   y = y0 + pDeltaY * pOffsetX / pDeltaX
		///>   y = y0 + pDeltaY * pRatio  (Here we wont calc pRatio first, in case that offsetX and DeltaX are intergers, leading to inaccurate division.
		///>								Instead, we calc pDeltaY * pOffsetX first
		///>   y = y0 + pOffsetY

		void* pDeltaX = pHelper->AllocData();
		void* pDeltaY = pHelper->AllocData();
		void* pOffsetX = pHelper->AllocData();
		void* pDeltaYxpOffsetX = pHelper->AllocData();
		void* pOffsetY = pHelper->AllocData();

		const void* x = m_InputX->GetValue(pAgent->GetSharedData());
		for (INT i = 0; i < sizeX - 1; ++i)
		{
			const void* x0 = m_KeyPointX->GetElement(pAgent->GetSharedData(), i);
			const void* x1 = m_KeyPointX->GetElement(pAgent->GetSharedData(), i + 1);

			///> Not in the range of this (x0, x1]
			if (pHelper->Compare(x, x1, OT_GREATER) && i < sizeX - 2)
				continue;
			const void* y0 = m_KeyPointY->GetElement(pAgent->GetSharedData(), i);
			const void* y1 = m_KeyPointY->GetElement(pAgent->GetSharedData(), i + 1);

			if (x0 == nullptr || x1 == nullptr || y0 == nullptr || y1 == nullptr)
			{
				DEBUG_LOG_INFO("Null value at keypoint index " << i <<"; ");
				continue;
			}

			pHelper->Calculate(pDeltaX, x1, x0, OT_SUB);
			pHelper->Calculate(pOffsetX, x, x0, OT_SUB);
			pHelper->Calculate(pDeltaY, y1, y0, OT_SUB);
			pHelper->Calculate(pDeltaYxpOffsetX, pDeltaY, pOffsetX, OT_MUL);
			pHelper->Calculate(pOffsetY, pDeltaYxpOffsetX, pDeltaX, OT_DIV);
			const void* res = pHelper->Calculate(y0, pOffsetY, OT_ADD);

			m_OutputY->SetValue(pAgent->GetSharedData(), res);
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
		pHelper->RecycleData(pDeltaX);
		pHelper->RecycleData(pDeltaY);
		pHelper->RecycleData(pOffsetX);
		pHelper->RecycleData(pOffsetY);
		pHelper->RecycleData(pDeltaYxpOffsetX);
		//if (m_DefaultChild != nullptr)
		//{
		//	DEBUG_LOG_INFO("Switch to default; ");
		//	ns = m_DefaultChild->Execute(pAgent);
		//}

		LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_OutputY, false);

		return ns;
	}

	void PiecewiseFunction::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID xVecType = CreateVariable(m_KeyPointX, "KeyPointX", data, false);
		if (s_ValidVecTypes.find(xVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for KeyPointX in PiecewiseFunction: " << xVecType << ERROR_END;
			return;
		}
		TYPEID yVecType = CreateVariable(m_KeyPointY, "KeyPointY", data, false);
		if (s_ValidVecTypes.find(yVecType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN << "Invalid type for KeyPointY in PiecewiseFunction: " << yVecType << ERROR_END;
			return;
		}

		TYPEID xType = CreateVariable(m_InputX, "InputX", data, true);
		if (s_ValidTypes.find(xType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for InputX in PiecewiseFunction: " << xType << ERROR_END;
			return;
		}
		TYPEID yType = CreateVariable(m_OutputY, "OutputY", data, true);
		if (s_ValidTypes.find(yType) == s_ValidTypes.end())
		{
			ERROR_BEGIN << "Invalid type for OutputY in PiecewiseFunction: " << yType << ERROR_END;
			return;
		}

		if (!Utility::IsElement(xType, xVecType))
		{
			ERROR_BEGIN << "Different types in PiecewiseFunction:  " << xType << " and " << xVecType << ERROR_END;
			return;
		}

		if (!Utility::IsElement(yType, yVecType))
		{
			ERROR_BEGIN << "Different types in PiecewiseFunction:  " << yType << " and " << yVecType << ERROR_END;
			return;
		}
	}
}