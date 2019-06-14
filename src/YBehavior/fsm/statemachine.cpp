#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/utility.h"
#include "YBehavior/fsm/metastate.h"
#include "YBehavior/logger.h"

namespace YBehavior
{
	StateMachine::StateMachine(FSMUIDType layer, FSMUIDType level, FSMUIDType index)
		: m_pDefaultState(nullptr)
		, m_EntryState(nullptr)
		, m_ExitState(nullptr)
	{
		m_UID.Layer = layer;
		m_UID.Level = level;
		m_UID.Machine = index;
		m_UID.State = 0;
	}

	StateMachine::~StateMachine()
	{
		for (auto it = m_AllStates.begin(); it != m_AllStates.end(); ++it)
		{
			delete *it;
		}
	}

	void StateMachine::InsertTrans(const TransitionMapKey& k, const TransitionMapValue& v)
	{
		if (v.toState == nullptr)
			return;
		auto res = m_TransitionMap.insert(std::pair< TransitionMapKey, TransitionMapValue>(k, v));
		if (res.second)
		{
			if (k.fromState != nullptr)
			{
				if (m_States.insert(k.fromState).second)
				{
					m_AllStates.push_back(k.fromState);
					k.fromState->GetUID().Value = m_UID.Value;
				}
			}
			if (m_States.insert(v.toState).second)
			{
				m_AllStates.push_back(v.toState);
				v.toState->GetUID().Value = m_UID.Value;
			}
		}
	}

	bool StateMachine::GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result)
	{
		TransitionMapKey key;
		if (pCurState != nullptr)
		{
			///> First find CurState->XXX
			key.fromState = pCurState;
			key.trans = context.GetTransition();
			auto it = m_TransitionMap.find(key);
			if (it == m_TransitionMap.end())
			{
				///> Then find AnyState->XXX
				key.fromState = nullptr;
				it = m_TransitionMap.find(key);
			}

			if (it != m_TransitionMap.end())
			{
				result.pFromState = key.fromState;
				result.trans = key.trans;
				result.pToState = it->second.toState;
				return true;
			}
		}
		return false;
	}

	void StateMachine::SetSpecialState(MachineState* pState)
	{
		if (pState == nullptr)
			return;

		if (pState->GetType() == MST_Entry)
		{
			if (m_EntryState != nullptr)
			{
				ERROR_BEGIN << "Duplicated Entry." << ERROR_END;
				return;
			}
			m_EntryState = pState;
			m_AllStates.push_back(pState);
		}
		else if (pState->GetType() == MST_Exit)
		{
			if (m_ExitState != nullptr)
			{
				ERROR_BEGIN << "Duplicated Exit." << ERROR_END;
				return;
			}
			m_ExitState = pState;
			m_AllStates.push_back(pState);
		}
	}

	void StateMachine::CheckDefault(MachineContext& context)
	{
		if (m_pDefaultState != nullptr && m_UID.Level > context.GetCurStateStack().size())
		{

		}
	}

	void StateMachine::Update(float fDeltaT, MachineContext& context)
	{
		LOG_BEGIN << "Update Machine" << LOG_END;
		///> There's a transition
		if (context.GetTransition().IsValid() && context.GetCurStateStack().size() > 0)
		{
			LOG_BEGIN << "Start Trans: " << context.GetTransition().GetEvent() << LOG_END;
			TransitionResult res;
			_Trans(context.GetCurStateStack().begin(), context, res);
			
			context.GetTransition().Reset();
		}
		if (context.GetCurStateStack().size() > 0)
		{
			LOG_BEGIN << "Update State In Machine" << LOG_END;
			(*context.GetCurStateStack().rbegin())->OnUpdate(fDeltaT, context);
		}
	}

	bool _CompareState(const MachineState* left, const MachineState* right)
	{
		return left->GetName() < right->GetName();
	}

	void StateMachine::OnLoadFinish()
	{
		StdVector<MachineState*> l(m_States.begin(), m_States.end());
		std::sort(l.begin(), l.end(), _CompareState);
		int index = 0;
		for (auto it = l.begin(); it != l.end(); )
		{
			(*it)->GetUID().State = ++index;
		}
	}

	bool StateMachine::_TryEnterDefault(MachineContext& context)
	{
		if (m_pDefaultState)
		{
			context.GetCurStateStack().push_back(m_pDefaultState);
			///> Enter failed
			if (m_pDefaultState->OnEnter(context) == MRR_Exit)
			{
				context.GetCurStateStack().erase(--context.GetCurStateStack().end());
			}
			else
			{
				///> Enter Default Successfully
				return true;
			}
		}
		return false;
	}
	bool StateMachine::_Trans(CurrentStateType::const_iterator it, MachineContext& context, TransitionResult& res)
	{
		MachineState* pCur = *it;
		if (pCur == nullptr)
			return false;
		if (this->GetTransition(pCur, context, res))
		{
			///> Exit from stack top
			for (auto it2 = context.GetCurStateStack().rbegin(); it2 != context.GetCurStateStack().rend(); ++it2)
			{
				(*it2)->OnExit(context);
				if ((*it2) == pCur)
				{
					context.GetCurStateStack().erase(it, context.GetCurStateStack().end());
					break;
				}
			}

			///> Try Enter new state
			context.GetCurStateStack().push_back(res.pToState);
			///> Enter failed
			if (res.pToState->OnEnter(context) == MRR_Exit)
			{
				context.GetCurStateStack().erase(--context.GetCurStateStack().end());
				///> Try Enter Default state
				if (_TryEnterDefault(context))
					return true;
				///> This machine will break
				res.pToState->OnExit(context);
				return false;
			}
			return true;
		}
		else
		{
			///> Go to next sub machine
			if (pCur->GetType() == MST_Meta)
			{
				StateMachine * pSubMachine = static_cast<MetaState*>(pCur)->GetMachine();
				++it;
				if (it != context.GetCurStateStack().end())
				{
					///> Sub Machine break
					if (!pSubMachine->_Trans(it, context, res))
					{
						context.GetCurStateStack().erase(--context.GetCurStateStack().end());
						///> Try Enter Default state
						if (_TryEnterDefault(context))
							return true;
						///> This machine will break
						res.pToState->OnExit(context);
						return false;
					}
				}
			}
		}

		///> Maybe it's an invalid trans, just ignore it
		return true;
	}
	MachineRunRes StateMachine::OnEnter(MachineContext& context)
	{
		LOG_BEGIN << "EnterMachine" << LOG_END;
		if (m_EntryState)
			m_EntryState->OnUpdate(0, context);

		///> Enter default state
		if (m_pDefaultState)
		{
			context.GetCurStateStack().push_back(m_pDefaultState);
			return m_pDefaultState->OnEnter(context);
		}
		else
		{
			return MRR_Exit;
		}
	}

	MachineRunRes StateMachine::OnExit(MachineContext& context)
	{
		LOG_BEGIN << "ExitMachine" << LOG_END;
		if (m_ExitState)
			m_ExitState->OnUpdate(0, context);
		return MRR_Normal;
	}

	FSM::FSM(const STRING& name)
		: m_Name(name)
		, m_Version(nullptr)
		, m_pMachine(nullptr)
	{

	}

	FSM::~FSM()
	{
		if (m_pMachine)
			delete m_pMachine;
	}

	YBehavior::StateMachine* FSM::CreateMachine()
	{
		if (m_pMachine)
			delete m_pMachine;
		m_pMachine = new StateMachine(1, 1, 1);
		return m_pMachine;
	}

}