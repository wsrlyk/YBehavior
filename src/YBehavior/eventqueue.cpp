#include "YBehavior/eventqueue.h"
#include "YBehavior/utility.h"
#include "YBehavior/tools/objectpool.h"
#include "YBehavior/fsm/behavior.h"
#include <algorithm>

namespace YBehavior
{
	void EventQueue::Event::Clear()
	{
#define DATA_VARIABLE_CLEAR(TYPE)\
	if (pVec##TYPE != nullptr)\
	{\
		pVec##TYPE->clear();\
		ObjectPoolStatic<StdVector<TYPE>>::Recycle(pVec##TYPE);\
		pVec##TYPE = nullptr;\
	}
		///> pVecInt = nullptr;
		FOR_EACH_REGISTER_TYPE(DATA_VARIABLE_CLEAR);
		nameHash = 0;
		name = Utility::StringEmpty;
	}

	void EventQueue::Event::Recycle()
	{
		Clear();
		ObjectPoolStatic<EventQueue::Event>::Recycle(this);
	}
#define DEFINE_PUSH_FUNCTION(TYPE)\
void EventQueue::Event::Push(const TYPE& data)\
{\
	if (!pVec##TYPE)\
		pVec##TYPE = ObjectPoolStatic<StdVector<TYPE>>::Get();\
	pVec##TYPE->push_back(data);\
}\
void EventQueue::Event::Assign(const StdVector<TYPE>& data)\
{\
	if (!pVec##TYPE)\
		pVec##TYPE = ObjectPoolStatic<StdVector<TYPE>>::Get();\
	pVec##TYPE->assign(data.begin(), data.end()); \
}

	FOR_EACH_REGISTER_TYPE(DEFINE_PUSH_FUNCTION);

	////////////////////////////////////////////////////////////////////////
	EventQueue::EventQueue(const Behavior* pBehavior)
		: m_pBehavior(pBehavior)
	{

	}

	EventQueue::~EventQueue()
	{
		ClearAll();
	}

	void EventQueue::Clear(StdVector<STRING>* pClearedEvents)
	{
		if (m_Events.empty())
			return;
		class RemoveNotNotClear
		{
			StdVector<STRING>* pResults{};
		public:
			RemoveNotNotClear(StdVector<STRING>* results)
				: pResults(results){}

			bool operator()(Event* pData)
			{
				if (pData->notClear)
					return false;
				if (pResults)
					pResults->emplace_back(pData->name);
				pData->Recycle();
				//ObjectPoolStatic<EventQueue::Event>::Recycle(pData);
				return true;
			}
		};
		m_Events.erase(std::remove_if(m_Events.begin(), m_Events.end(), RemoveNotNotClear(pClearedEvents)), m_Events.end());
	}
	void EventQueue::ClearAll(StdVector<STRING>* pClearedEvents)
	{
		if (m_Events.empty())
			return;

		if (pClearedEvents)
		{
			for (auto it = m_Events.begin(); it != m_Events.end(); ++it)
			{
				pClearedEvents->emplace_back((*it)->name);
			}
		}
		for (auto it = m_Events.begin(); it != m_Events.end(); ++it)
		{
			(*it)->Recycle();
			//ObjectPoolStatic<EventQueue::Event>::Recycle(*it);
		}
		m_Events.clear();
	}

	EventQueue::Event* EventQueue::Create(const STRING& name)
	{
		auto hash = Utility::Hash(name);
		UINT count = 0;
		if (m_pBehavior)
		{
			count = m_pBehavior->IsValidEvent(hash);
			if (count == 0)
				return nullptr;
		}
		EventQueue::Event* pEvent = nullptr;
		if (count == 1)
		{
			for (auto it = m_Events.begin(); it != m_Events.end(); ++it)
			{
				if ((*it)->nameHash == hash)
				{
					pEvent = *it;
					pEvent->Clear();
					m_Events.erase(it);
					break;
				}
			}
		}
		if (!pEvent)
		{
			pEvent = ObjectPoolStatic<EventQueue::Event>::Get();
		}
		m_Events.emplace_back(pEvent);
		pEvent->name = name;
		pEvent->nameHash = hash;
		return pEvent;
	}

	const EventQueue::Event* EventQueue::TryGetLast() const
	{
		if (m_Events.empty())
			return nullptr;
		auto pEvent = *m_Events.rbegin();
		return pEvent;
	}

	EventQueue::Event* EventQueue::TryGetFirstAndPop(size_t& startIdx, UINT nameHash)
	{
		for (auto it = m_Events.begin() + startIdx, end = m_Events.end(); it != end; ++it)
		{
			if ((*it)->nameHash == nameHash)
			{
				auto res = *it;
				startIdx = it - m_Events.begin();
				m_Events.erase(it);
				return res;
			}
		}
		return nullptr;
	}

	EventQueue::Event* EventQueue::TryGetAndPop(size_t& startIdx,
		StdVector<UINT>::const_iterator begin,
		StdVector<UINT>::const_iterator end,
		size_t& offset)
	{
		for (auto it = m_Events.begin() + startIdx, e = m_Events.end(); it != e; ++it)
		{
			auto hash = (*it)->nameHash;
			auto it2 = std::find(begin, end, hash);
			if (it2 != end)
			{
				auto res = *it;
				startIdx = it - m_Events.begin();
				offset = it2 - begin;
				m_Events.erase(it);
				return res;
			}
		}
		return nullptr;
	}
}