#include "YBehavior/nodes/handleevent.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/pin.h"
#include "YBehavior/pincreation.h"
#include "YBehavior/eventqueue.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/fsm/context.h"

namespace YBehavior
{
	void HandleEventContext::_OnInit()
	{
		CompositeNodeContext::_OnInit();
		m_pTargetHashes = nullptr;
		m_Hashes.clear();
		m_Idx = 0;
	}

	NodeState HandleEventContext::_Return(HandleEvent* pNode, NodeState lastState)
	{
		///> return failure when there's no event.
		if (m_Stage == 0)
			return NS_FAILURE;

		/////> only return the child state when everything is single
		//if (pNode->m_Type == HandleEventType::LATEST &&
		//	m_pChildren->size() == 1 &&
		//	m_pTargetHashes->size() == 1)
		//	return lastState;

		///> return success when there has one or more events.
		return NS_SUCCESS;
	}
	NodeState HandleEventContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		HandleEvent* pNode = static_cast<HandleEvent*>(m_pNode);
		if (!pAgent->GetEventQueue()->IsDirty())
			return _Return(pNode, lastState);

		if (m_Stage == 0)
		{
			YB_LOG_PIN_BEFORE_IF_HAS_DEBUG_POINT(pNode->m_Events);

			if (!pNode->m_Events)
			{
				m_pTargetHashes = nullptr;
			}
			else if (!pNode->m_Events->IsConst())
			{
				pNode->_GetHashes(m_Hashes, pAgent->GetMemory());
				m_pTargetHashes = &m_Hashes;
			}
			else
			{
				m_pTargetHashes = &pNode->m_Hashes;
			}
		}

		EventQueue::Event* pEvent;
		size_t offset;
		if (m_pTargetHashes == nullptr)
			pEvent = pAgent->GetEventQueue()->TryGetFirstAndPop();
		else if (m_pTargetHashes->size() == 1)
			pEvent = pAgent->GetEventQueue()->TryGetFirstAndPop(m_Idx, (*m_pTargetHashes)[0]);
		else
			pEvent = pAgent->GetEventQueue()->TryGetAndPop(m_Idx, m_pTargetHashes->begin(), m_pTargetHashes->end(), offset);
		
		if (pEvent)
		{
			YB_LOG_INFO_WITH_END("Handle " << pEvent->name);

			if (pNode->m_bHasParam)
			{
				if (pNode->m_Current)
				{
					pNode->m_Current->SetValue(pAgent->GetMemory(), pEvent->name);
				}
				if (pNode->m_Int && pEvent->pVecInt)
				{
					pNode->m_Int->SetValue(pAgent->GetMemory(), *pEvent->pVecInt);
				}
				if (pNode->m_Float && pEvent->pVecFloat)
				{
					pNode->m_Float->SetValue(pAgent->GetMemory(), *pEvent->pVecFloat);
				}
				if (pNode->m_Bool && pEvent->pVecBool)
				{
					pNode->m_Bool->SetValue(pAgent->GetMemory(), *pEvent->pVecBool);
				}
				if (pNode->m_String && pEvent->pVecString)
				{
					pNode->m_String->SetValue(pAgent->GetMemory(), *pEvent->pVecString);
				}
				if (pNode->m_Vector3 && pEvent->pVecVector3)
				{
					pNode->m_Vector3->SetValue(pAgent->GetMemory(), *pEvent->pVecVector3);
				}
				if (pNode->m_Entity && pEvent->pVecEntityWrapper)
				{
					pNode->m_Entity->SetValue(pAgent->GetMemory(), *pEvent->pVecEntityWrapper);
				}
				if (pNode->m_Ulong && pEvent->pVecUlong)
				{
					pNode->m_Ulong->SetValue(pAgent->GetMemory(), *pEvent->pVecUlong);
				}
			}

			pEvent->Recycle();
			if (m_pChildren->size() == 1)
			{
				TreeNodePtr node = (*m_pChildren)[0];
				pAgent->GetTreeContext()->PushCallStack(node->CreateContext());
			}
			else
			{
				TreeNodePtr node = (*m_pChildren)[offset];
				pAgent->GetTreeContext()->PushCallStack(node->CreateContext());
			}
		}
		else
		{
			return _Return(pNode, lastState);
		}
		++m_Stage;
		return NS_RUNNING;
	}

	//////////////////////////////////////////////////////////////////////////////////////////
	static Bimap<HandleEventType, STRING> OperatorMap = {
	{ HandleEventType::LATEST, "Latest" },
	{ HandleEventType::EVERY, "Every" },
	};

	bool HandleEvent::OnLoaded(const pugi::xml_node& data)
	{
		PinCreation::CreatePinIfExist(this, m_Events, "Events", data);
		//if (!m_Events)
		//	return false;

		if (!PinCreation::GetValue(this, "Type", data, OperatorMap, m_Type))
			return false;

		PinCreation::CreatePinIfExist(this, m_Current, "Current", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_Int, "Int", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_Float, "Float", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_Bool, "Bool", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_String, "String", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_Vector3, "Vector3", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_Entity, "Entity", data, PinCreation::Flag::IsOutput);
		PinCreation::CreatePinIfExist(this, m_Ulong, "Ulong", data, PinCreation::Flag::IsOutput);

		m_bHasParam = m_Current || m_Int || m_Float || m_Bool || m_String || m_Vector3 || m_Entity || m_Ulong;

		return true;
	}

	bool HandleEvent::OnLoadFinish()
	{
		size_t eventCount = 0;
		if (m_Events && m_Events->IsConst())
			eventCount = m_Events->ArraySize(nullptr);
		else
			eventCount = 1;

		if (eventCount == 0)
		{
			ERROR_BEGIN_NODE_HEAD << "No Events" << ERROR_END;
			return false;
		}

		size_t childCount = m_Children->size();
		if (!(childCount == 1 || childCount == eventCount))
		{
			ERROR_BEGIN_NODE_HEAD << "Children count not match" << ERROR_END;
			return false;
		}

		if (m_Events && m_Events->IsConst())
		{
			_GetHashes(m_Hashes, nullptr);
			if (m_Root)
			{
				for (auto v : m_Hashes)
					m_Root->RegiseterEvent(v, m_Type == HandleEventType::LATEST ? 1 : 2);
			}
		}
		return true;
	}

	void HandleEvent::_GetHashes(StdVector<UINT>& hashes, IMemory* pMemory)
	{
		auto pValue = m_Events->GetValue(pMemory);
		const StdVector<STRING>& events = *(const StdVector<STRING>*)pValue;
		for (auto it = events.begin(); it != events.end(); ++it)
		{
			hashes.emplace_back(Utility::Hash(*it));
		}
	}
}
