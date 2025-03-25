#ifndef _YBEHAVIOR_TRANSITION_H_
#define _YBEHAVIOR_TRANSITION_H_

#include "YBehavior/types/types.h"
#include "YBehavior/types/smallmap.h"
#include <list>

namespace YBehavior
{
	class MachineState;
	class ConditionMgr
	{
		small_map<STRING, int> m_Map;
	public:
		int GetConditionValue(const STRING& cond);
		bool TryGetConditionValue(const STRING& cond, int& value);
	};

	class Transition
	{
	protected:
		ULONG m_Conditions;
		ConditionMgr* m_pConditionMgr;
	public:
		Transition(ConditionMgr* conditionMgr = nullptr);
		inline bool operator==(const Transition& _rhs) const;

		inline void Reset();
		inline void SetConditionMgr(ConditionMgr* conditionMgr);
		
		void Set(const STRING& e);
		bool TrySet(const STRING& e);

		bool UnSet(const STRING& e);
		bool UnSet(const Transition& other);
		bool ContainedBy(const Transition& other) const;
	};

	struct TransitionMapKey
	{
		MachineState* fromState{};
		Transition trans{};
		inline bool operator==(const TransitionMapKey& _rhs) const;
	};

	struct TransitionMapValue
	{
		MachineState* toState{};
	};

	struct TransitionData
	{
	protected:
		UINT m_UID;
	public:
		TransitionMapKey Key;
		TransitionMapValue Value;
		TransitionData(TransitionMapKey key, TransitionMapValue value, UINT uid);
		inline bool operator==(const TransitionData& _rhs) const;
		inline bool operator<(const TransitionData& _rhs) const;
	};

	class CanTransTeller
	{
		TransitionMapKey m_TransKey;
	public:
		CanTransTeller(const TransitionMapKey& trans);
		bool operator()(const TransitionData& other) const;
	};

	class StateMachine;
	struct TransitionResult
	{
		MachineState* pFromState{};
		MachineState* pToState{};
		Transition trans{};
		StateMachine* pMachine{};

		std::list<MachineState*> lcaRoute;
	};

	enum TransQueueOp
	{
		TQO_None,
		TQO_Enter,
		TQO_Exit,
	};

	struct TransQueueData
	{
		MachineState* pState;
		TransQueueOp op;

		TransQueueData(MachineState* state);

		TransQueueData(MachineState* state, TransQueueOp o);
	};
}

#endif