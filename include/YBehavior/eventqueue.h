#ifndef _YBEHAVIOR_EVENTQUEUE_H_
#define _YBEHAVIOR_EVENTQUEUE_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"
#include "YBehavior/types/smallmap.h"
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
		//TODO: If in most cases, agents share common lists of valid events, 
		// which means RegisterEvent is called very few times, 
		// then there's no need to copy the common lists to here in every Init.
		small_map<UINT, UINT> m_ValidEvents;
	public:
		~EventQueue();

		void Init(const small_map<UINT, UINT>& validEvents);
		void RegisterEvent(const STRING& e, UINT count);

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