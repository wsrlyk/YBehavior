#ifndef _YBEHAVIOR_EVENTQUEUE_H_
#define _YBEHAVIOR_EVENTQUEUE_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"
namespace YBehavior
{
#define FOR_EACH_REGISTER_TYPE(func)    \
		func(Int);    \
		func(Float);    \
		func(String);    \
		func(Bool);    \
		func(Ulong);   \
		func(EntityWrapper);   \
		func(Vector3);

	class Behavior;
	class EventQueue
	{
	public:
		struct Event
		{
#define DATA_VARIABLE_DEFINE(TYPE) Vec##TYPE* pVec##TYPE{};
			///> VecInt* pVecInt;
			FOR_EACH_REGISTER_TYPE(DATA_VARIABLE_DEFINE);

			bool notClear{ false };
			UINT nameHash{};
			STRING name{};
#define DECLARE_PUSH_FUNCTION(TYPE)\
			void Push(const TYPE& data);\
			void Assign(const StdVector<TYPE>& data);

			FOR_EACH_REGISTER_TYPE(DECLARE_PUSH_FUNCTION);

			void Recycle();
			void Clear();
		};
	private:
		StdVector<Event*> m_Events;
		const Behavior* m_pBehavior;
	public:
		EventQueue(const Behavior* pBehavior);
		~EventQueue();
		void Clear(StdVector<STRING>* pClearedEvents = nullptr);
		void ClearAll(StdVector<STRING>* pClearedEvents = nullptr);
		Event* Create(const STRING& name);
		inline bool IsDirty() { return !m_Events.empty(); }

		///> DO NOT recycle Event after using it
		const Event* TryGetLast() const;
		///> MUST recycle Event after using it
		Event* TryGetFirstAndPop(size_t& startIdx, UINT nameHash);
		///> MUST recycle Event after using it
		Event* TryGetAndPop(size_t& startIdx,
							StdVector<UINT>::const_iterator begin,
							StdVector<UINT>::const_iterator end,
							size_t& offset);
	};
}

#endif