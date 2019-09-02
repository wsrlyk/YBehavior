#include "YBehavior/fsm/transition.h"
#include "YBehavior/utility.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"

namespace YBehavior
{
	int ConditionMgr::GetConditionValue(const STRING& cond)
	{
		auto it = m_Map.find(cond);
		if (it == m_Map.end())
		{
			int newValue = m_Map.size();
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

	bool Transition::ContainedBy(const Transition& other) const
	{
		return (m_Conditions & other.m_Conditions) == m_Conditions;
	}

	CanTransTeller::CanTransTeller(const TransitionMapKey& trans)
		: m_TransKey(trans)
	{

	}

	bool CanTransTeller::operator()(const TransitionData& other) const
	{
		return ((other.Key.fromState == m_TransKey.fromState || m_TransKey.fromState == nullptr)
			&& other.Key.trans.ContainedBy(m_TransKey.trans));
	}

}