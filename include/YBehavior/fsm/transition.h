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
		Transition(ConditionMgr* conditionMgr = nullptr) : m_Conditions(0), m_pConditionMgr(conditionMgr) {}
		inline bool operator==(const Transition& _rhs) const { return m_Conditions == _rhs.m_Conditions; }
		inline bool operator<(const Transition& _rhs) const { return m_Conditions < _rhs.m_Conditions; }

		inline void Reset() 
		{
			m_Conditions = 0; 
		}
		inline void SetConditionMgr(ConditionMgr* conditionMgr) 
		{
			m_pConditionMgr = conditionMgr; 
			m_Conditions = 0;
		}
		
		void Set(const STRING& e);
		bool TrySet(const STRING& e);

		bool UnSet(const STRING& e);
		bool UnSet(const Transition& other);
		bool ContainedBy(const Transition& other) const;
	};

	struct TransitionMapKey
	{
		MachineState* fromState;
		Transition trans;
		TransitionMapKey() : fromState(nullptr) {}
		inline bool operator==(const TransitionMapKey& _rhs) const
		{
			return fromState == _rhs.fromState && trans == _rhs.trans;
		}
		inline bool operator<(const TransitionMapKey& _rhs) const
		{
			return fromState < _rhs.fromState || (fromState == _rhs.fromState && trans < _rhs.trans);
		}
	};

	struct TransitionMapValue
	{
		MachineState* toState;
		TransitionMapValue() : toState(nullptr) {}
	};

	struct TransitionData
	{
		TransitionMapKey Key;
		TransitionMapValue Value;
		TransitionData(TransitionMapKey key, TransitionMapValue value)
			: Key(key)
			, Value(value)
		{

		}
		inline bool operator==(const TransitionData& _rhs) const
		{
			return Key == _rhs.Key;
		}
		inline bool operator<(const TransitionData& _rhs) const
		{
			return Key < _rhs.Key;
		}
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
		MachineState* pFromState;
		MachineState* pToState;
		Transition trans;
		StateMachine* pMachine;

		std::list<MachineState*> lcaRoute;

		TransitionResult()
			: pFromState(nullptr)
			, pToState(nullptr)
			, pMachine(nullptr)
		{
		}
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

		TransQueueData(MachineState* state)
		{
			pState = state;
			op = TQO_None;
		}

		TransQueueData(MachineState* state, TransQueueOp o)
		{
			pState = state;
			op = o;
		}
	};
}

#endif