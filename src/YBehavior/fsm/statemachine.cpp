#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/utility.h"
#include "YBehavior/fsm/metastate.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"

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
				}
			}
			if (m_States.insert(v.toState).second)
			{
				m_AllStates.push_back(v.toState);
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
			key.trans = context.GetTransition().Get();
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
				result.pMachine = this;
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
		if (m_pDefaultState != nullptr && m_UID.Level > context.GetCurStatesStack().size())
		{

		}
	}

	void StateMachine::Update(float fDeltaT, AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();
		LOG_BEGIN << "Update Machine" << LOG_END;
		///> There's a transition
		if (context.GetTransition().HasTransition() && context.GetCurStatesStack().size() > 0)
		{
			if (context.GetTransition().transferStage == MTS_None)
			{
				LOG_BEGIN << "Start Trans: " << context.GetTransition().Get().GetEvent() << LOG_END;
				context.GetTransition().Lock();
			}
			///> Trans is not finished in this tick
			if (!_Trans(pAgent))
				return;
			
			context.GetTransition().Reset();
		}
		if (context.GetCurStatesStack().size() > 0)
		{
			LOG_BEGIN << "Update State In Machine" << LOG_END;
			(*context.GetCurStatesStack().rbegin())->OnUpdate(fDeltaT, pAgent);
		}
		else
		{
			context.GetTransition().transferRunRes = OnEnter(pAgent);
		}
	}

	bool _CompareState(const MachineState* left, const MachineState* right)
	{
		return left->GetSortValue() < right->GetSortValue();
	}

	void StateMachine::OnLoadFinish()
	{
		StdVector<MachineState*> l(m_AllStates.begin(), m_AllStates.end());
		std::sort(l.begin(), l.end(), _CompareState);
		int index = 0;
		for (auto it = l.begin(); it != l.end(); ++it)
		{
			(*it)->GetUID().Value = m_UID.Value;
			(*it)->GetUID().State = ++index;
		}
	}

	bool StateMachine::_TryEnterDefault(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();
		if (m_pDefaultState)
		{
			context.GetCurStatesStack().push_back(m_pDefaultState);
			///> Enter failed
			if (m_pDefaultState->OnEnter(pAgent) == MRR_Exit)
			{
				context.GetCurStatesStack().pop_back();
			}
			else
			{
				///> Enter Default Successfully
				return true;
			}
		}
		return false;
	}
	bool StateMachine::_Trans(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();

		TransitionContext& transContext = context.GetTransition();

		///> Find the From State
		if (transContext.transferStage <= MTS_None)
		{
			transContext.transferStage = MTS_None;
			StateMachine* pCurMachine = this;
			for (auto it = context.GetCurStatesStack().begin(); it != context.GetCurStatesStack().end(); ++it)
			{
				MachineState* pCurState = *it;
				if (pCurMachine->GetTransition(pCurState, context, transContext.transferResult))
				{
					///> Found
				}
				else
				{
					if (pCurState->GetType() == MST_Meta)
					{
						pCurMachine = static_cast<MetaState*>(pCurState)->GetMachine();
					}
					else
					{
						LOG_BEGIN << "Trans Event that cant trans to any states: " << context.GetTransition().Get().GetEvent() << LOG_END;
						return true;
					}
				}
			}
		}

		///> Exit the low level states
		if (transContext.transferStage <= MTS_Exit)
		{
			transContext.transferStage = MTS_Exit;
			while (!context.GetCurStatesStack().empty())
			{
				MachineState* pCurState = context.GetCurStatesStack().back();
				///> The tree not finally return yet
				transContext.transferRunRes = pCurState->OnExit(pAgent);
				if (transContext.transferRunRes != MRR_Normal)
				{
					return false;
				}
				context.GetCurStatesStack().pop_back();
				if (pCurState == transContext.transferResult.pFromState)
					break;
			}
		}

		///> Enter the new state
		if (transContext.transferStage <= MTS_Enter)
		{
			if (transContext.transferStage < MTS_Enter)
			{
				transContext.transferStage = MTS_Enter;
				context.GetCurStatesStack().push_back(transContext.transferResult.pToState);
			}
			transContext.transferRunRes = transContext.transferResult.pToState->OnEnter(pAgent);
			switch (transContext.transferRunRes)
			{
			case YBehavior::MRR_Normal:
				return true;
			case YBehavior::MRR_Exit:
			{
				context.GetCurStatesStack().pop_back();

			}
				break;
			case YBehavior::MRR_Running:
			case YBehavior::MRR_Break:
				return false;
			default:
				break;
			}
		}

		///> Enter default state
		if (transContext.transferStage <= MTS_Default)
		{
			transContext.transferStage = MTS_Default;

			while (!context.GetCurStatesStack().empty())
			{
				MachineState* pCurState = context.GetCurStatesStack().back();
				StateMachine* pCurMachine = nullptr;
				if (transContext.transferRunRes == MRR_Running || transContext.transferRunRes == MRR_Break)
				{
					transContext.transferRunRes = pCurState->OnEnter(pAgent);
				}
				else if (pCurState->GetType() == MST_Meta)
				{
					pCurMachine = static_cast<MetaState*>(pCurState)->GetMachine();
					if (pCurMachine->m_pDefaultState)
					{
						context.GetCurStatesStack().push_back(pCurMachine->m_pDefaultState);
						///> Enter failed
						transContext.transferRunRes = pCurMachine->m_pDefaultState->OnEnter(pAgent);
					}
				}
				else
				{
					ERROR_BEGIN << "State in the stack is not a SubMachine: " << pCurState->ToString() << ERROR_END;
					return false;
				}

				switch (transContext.transferRunRes)
				{
				case MRR_Running:
				case MRR_Break:
					return false;
				case MRR_Normal:
					return true;
				case MRR_Exit:
					///> Pop cur default state
					context.GetCurStatesStack().pop_back();

					///> Pop SubMachine
					context.GetCurStatesStack().pop_back();
				default:
					break;
				}
			}

			///> Root machine
			if (m_pDefaultState)
			{
				context.GetCurStatesStack().push_back(m_pDefaultState);
				///> Enter failed
				transContext.transferRunRes = m_pDefaultState->OnEnter(pAgent);
				switch (transContext.transferRunRes)
				{
				case MRR_Running:
				case MRR_Break:
					return false;
				case MRR_Normal:
					return true;
				case MRR_Exit:
					///> Pop cur default state
					context.GetCurStatesStack().pop_back();
				default:
					break;
				}
			}
		}

		///> Should not run to here
		transContext.transferRunRes = MRR_Normal;
		return false;
	}

	bool StateMachine::_Trans(CurrentStatesType::const_iterator it, AgentPtr pAgent, TransitionResult& res)
	{
		MachineState* pCur = *it;
		if (pCur == nullptr)
			return false;
		MachineContext& context = *pAgent->GetMachineContext();
		if (this->GetTransition(pCur, context, res))
		{
			///> Exit from stack top
			for (auto it2 = context.GetCurStatesStack().rbegin(); it2 != context.GetCurStatesStack().rend(); ++it2)
			{
				(*it2)->OnExit(pAgent);
				if ((*it2) == pCur)
				{
					context.GetCurStatesStack().erase(it, context.GetCurStatesStack().end());
					break;
				}
			}

			///> Try Enter new state
			context.GetCurStatesStack().push_back(res.pToState);
			///> Enter failed
			if (res.pToState->OnEnter(pAgent) == MRR_Exit)
			{
				context.GetCurStatesStack().pop_back();
				///> Try Enter Default state
				if (_TryEnterDefault(pAgent))
					return true;
				///> This machine will break
				res.pToState->OnExit(pAgent);
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
				if (it != context.GetCurStatesStack().end())
				{
					///> Sub Machine break
					if (!pSubMachine->_Trans(it, pAgent, res))
					{
						context.GetCurStatesStack().pop_back();
						///> Try Enter Default state
						if (_TryEnterDefault(pAgent))
							return true;
						///> This machine will break
						res.pToState->OnExit(pAgent);
						return false;
					}
				}
			}
		}

		///> Maybe it's an invalid trans, just ignore it
		return true;
	}
	MachineRunRes StateMachine::OnEnter(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();
		if (m_EntryState)
		{
			if (context.CanRun(m_EntryState))
			{
				if (context.GetCurRunningState() == nullptr)
					LOG_BEGIN << "EnterMachine" << LOG_END;

				MachineRunRes res = m_EntryState->OnUpdate(0, pAgent);
				if (res == MRR_Running || res == MRR_Break)
					return res;
			}
		}

		///> Enter default state
		if (m_pDefaultState)
		{
			context.GetCurStatesStack().push_back(m_pDefaultState);
			return m_pDefaultState->OnEnter(pAgent);
		}
		else
		{
			return MRR_Exit;
		}
	}

	MachineRunRes StateMachine::OnExit(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();

		if (m_ExitState)
		{
			if (context.CanRun(m_EntryState))
			{
				if (context.GetCurRunningState() == nullptr)
					LOG_BEGIN << "ExitMachine" << LOG_END;
				return m_ExitState->OnUpdate(0, pAgent);
			}
		}
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

	void FSM::Update(float fDeltaT, AgentPtr pAgent)
	{
		if (m_pMachine)
			m_pMachine->Update(fDeltaT, pAgent);
	}

	//////////////////////////////////////////////////////////////////////////

	TransitionContext::TransitionContext(const STRING& e)
		: m_Trans(e)
		, m_bLock(false)
		, transferStage(MTS_None)
		, transferRunRes(MRR_Normal)
	{

	}

	TransitionContext::TransitionContext()
		: m_Trans()
		, m_bLock(false)
		, transferStage(MTS_None)
		, transferRunRes(MRR_Normal)
	{

	}

	MachineContext::MachineContext()
		: m_pMapping(nullptr)
		, m_pCurRunningState(nullptr)
	{

	}

	void MachineContext::Reset()
	{
		m_pMapping = nullptr;
		m_pCurRunningState = nullptr;
		m_CurStates.clear();
		m_Trans.Reset();
	}

}