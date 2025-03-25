#include "YBehavior/fsm/transition.h"
#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/fsm/statemachine.h"

namespace YBehavior
{
	int ConditionMgr::GetConditionValue(const STRING& cond)
	{
		auto it = m_Map.find(cond);
		if (it == m_Map.end())
		{
			int newValue = (int)m_Map.size();
			m_Map[cond] = newValue;
			return newValue;
		}
		return it->second;
	}

	bool ConditionMgr::TryGetConditionValue(const STRING& cond, int& value)
	{
		auto it = m_Map.find(cond);
		if (it == m_Map.end())
		{
			return false;
		}
		value = it->second;
		return true;
	}

	Transition::Transition(ConditionMgr* conditionMgr /*= nullptr*/)
		: m_Conditions(0)
		, m_pConditionMgr(conditionMgr) 
	{
	}

	bool Transition::operator==(const Transition& _rhs) const
	{
		return m_Conditions == _rhs.m_Conditions;
	}

	void Transition::Reset()
	{
		m_Conditions = 0;
	}

	void Transition::SetConditionMgr(ConditionMgr* conditionMgr)
	{
		m_pConditionMgr = conditionMgr;
		m_Conditions = 0;
	}

	void Transition::Set(const STRING& e)
	{
		if (m_pConditionMgr != nullptr)
		{
			int v = m_pConditionMgr->GetConditionValue(e);
			m_Conditions |= ((ULONG)1 << v);
		}
	}

	bool Transition::TrySet(const STRING& e)
	{
		int v;
		if (m_pConditionMgr->TryGetConditionValue(e, v))
		{
			m_Conditions |= ((ULONG)1 << v);
			return true;
		}
		return false;
	}

	bool Transition::UnSet(const STRING& e)
	{
		int v;
		if (m_pConditionMgr->TryGetConditionValue(e, v))
		{
			m_Conditions &= (~((ULONG)1 << v));
			return true;
		}
		return false;
	}

	bool Transition::UnSet(const Transition& other)
	{
		bool res = (m_Conditions & other.m_Conditions) != 0;
		m_Conditions &= (~other.m_Conditions);
		return res;
	}

	bool Transition::ContainedBy(const Transition& other) const
	{
		return (m_Conditions & other.m_Conditions) == m_Conditions;
	}

	bool TransitionMapKey::operator==(const TransitionMapKey& _rhs) const
	{
		return fromState == _rhs.fromState && trans == _rhs.trans;
	}

	TransitionData::TransitionData(TransitionMapKey key, TransitionMapValue value, UINT uid)
		: m_UID(uid)
		, Key(key)
		, Value(value)
	{

	}

	bool TransitionData::operator==(const TransitionData& _rhs) const
	{
		return Key == _rhs.Key;
	}

	bool TransitionData::operator<(const TransitionData& _rhs) const
	{
		return m_UID < _rhs.m_UID;
	}

	CanTransTeller::CanTransTeller(const TransitionMapKey& trans)
		: m_TransKey(trans)
	{

	}

	bool IsAncestor(MachineState* child, MachineState* ancestor)
	{
		if (child == nullptr || ancestor == nullptr || ancestor->GetType() != MST_Meta)
			return false;
		auto childMachine = child->GetParentMachine();
		auto ancestorMachine = ancestor->GetParentMachine();
		while (childMachine && ancestorMachine && childMachine->GetLevel() > ancestorMachine->GetLevel())
		{
			childMachine = childMachine->GetParentMachine();
			if (childMachine == ancestorMachine)
				return true;
		}
		return false;
	}

	bool CanTransTeller::operator()(const TransitionData& other) const
	{
		return ((other.Key.fromState == m_TransKey.fromState || m_TransKey.fromState == nullptr || IsAncestor(m_TransKey.fromState, other.Key.fromState))
			&& other.Key.trans.ContainedBy(m_TransKey.trans));
	}


	TransQueueData::TransQueueData(MachineState* state)
	{
		pState = state;
		op = TQO_None;
	}

	TransQueueData::TransQueueData(MachineState* state, TransQueueOp o)
	{
		pState = state;
		op = o;
	}
}