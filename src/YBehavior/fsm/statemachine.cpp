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
		, m_pRootMachine(nullptr)
		, m_pMetaState(nullptr)
	{
		m_UID.Layer = layer;
		m_UID.Level = level;
		m_UID.Machine = index;
		m_UID.State = 0;
	}

	StateMachine::~StateMachine()
	{
	}

	void StateMachine::SetMetaState(MetaState* pState)
	{
		if (pState == nullptr)
			return;

		m_pMetaState = pState;
		m_pRootMachine = m_pMetaState->GetParentMachine()->GetRootMachine();
	}

	StateMachine* StateMachine::GetParentMachine() const
	{
		if (m_pMetaState == nullptr)
			return nullptr;

		return m_pMetaState->GetParentMachine();
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
			GetRootMachine()->PushState(pState);
		}
		else if (pState->GetType() == MST_Exit)
		{
			if (m_ExitState != nullptr)
			{
				ERROR_BEGIN << "Duplicated Exit." << ERROR_END;
				return;
			}
			m_ExitState = pState;
			GetRootMachine()->PushState(pState);
		}
	}

	//bool _CompareState(const MachineState* left, const MachineState* right)
	//{
	//	return left->GetSortValue() < right->GetSortValue();
	//}

	void StateMachine::OnLoadFinish()
	{
	}

	bool StateMachine::TryEnterDefault(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();
		if (m_pDefaultState)
		{
			context.SetCurState(m_pDefaultState);
			context.GetTransition().transferRunRes = m_pDefaultState->OnEnter(pAgent);

			return true;
		}
		return false;
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
			///> Only go to default when there's no trans target or trans target is this
			if (context.GetTransition().transferResult.pToState != nullptr)// &&
//					context.GetTransition().transferResult.pToState != this->GetMetaState())
				return MRR_Normal;
			if (context.CanRun(m_pDefaultState))
			{
				context.SetCurState(m_pDefaultState);
				return m_pDefaultState->OnEnter(pAgent);
			}
		}
		else
		{
			return MRR_Exit;
		}

		return MRR_Normal;
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

	RootMachine* FSM::CreateMachine()
	{
		if (m_pMachine)
			delete m_pMachine;
		m_pMachine = new RootMachine(1);
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
		: m_pCurState (nullptr)
		, m_pMapping(nullptr)
		, m_pCurRunningState(nullptr)
	{

	}

	void MachineContext::Reset()
	{
		m_pMapping = nullptr;
		m_pCurRunningState = nullptr;
		m_pCurState = nullptr;
		m_Trans.Reset();
	}

	void MachineContext::PopCurState()
	{
		if (m_pCurState == nullptr)
			return;

		m_pCurState = m_pCurState->GetParentMachine()->GetMetaState();
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	RootMachine::RootMachine(FSMUIDType layer)
		: StateMachine(layer, 1, 1)
	{
		m_pRootMachine = this;
	}

	RootMachine::~RootMachine()
	{
		for (auto it = m_AllStates.begin(); it != m_AllStates.end(); ++it)
		{
			delete *it;
		}
	}

	bool RootMachine::InsertState(MachineState* pState)
	{
		if (pState == nullptr)
			return false;

		if (m_States.insert(std::pair<STRING, MachineState*>(pState->GetName(), pState)).second)
		{
			m_AllStates.push_back(pState);
			return true;
		}

		return false;
	}

	void RootMachine::PushState(MachineState* pState)
	{
		m_AllStates.push_back(pState);
	}

	bool RootMachine::InsertTrans(const TransitionMapKey& k, const TransitionMapValue& v)
	{
		if (v.toState == nullptr)
			return false;
		auto res = m_TransitionMap.insert(std::pair< TransitionMapKey, TransitionMapValue>(k, v));
		return res.second;
	}

	MachineState* _FindLCA(MachineState* pA, MachineState* pB)
	{
		if (pA == nullptr || pB == nullptr)
			return nullptr;

		MachineState* pDeeper;
		MachineState* pShallower;
		if (pA->GetUID().Level > pB->GetUID().Level)
		{
			pDeeper = pA;
			pShallower = pB;
		}
		else
		{
			pDeeper = pB;
			pShallower = pA;
		}

		MachineState* pC = pDeeper;
		MachineState* pD = pShallower;
		for (int i = pDeeper->GetUID().Level - pShallower->GetUID().Level; i > 0; --i)
		{
			pC = pC->GetParentMachine()->GetMetaState();
		}

		while (pC != pD && pC != nullptr)
		{
			pC = pC->GetParentMachine()->GetMetaState();
			pD = pD->GetParentMachine()->GetMetaState();
		}

		return pC;
	}

	bool RootMachine::GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result)
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
				result.pLCA = _FindLCA(pCurState, result.pToState);
				return true;
			}
		}
		return false;
	}

	YBehavior::MachineState* RootMachine::FindState(const STRING& name)
	{
		auto it = m_States.find(name);
		if (it == m_States.end())
			return nullptr;
		return it->second;
	}

	void RootMachine::OnLoadFinish()
	{
	}

	void RootMachine::Update(float fDeltaT, AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();
		LOG_BEGIN << "Update Machine" << LOG_END;
		///> There's a transition
		if (context.GetTransition().HasTransition() && context.GetCurState() != nullptr)
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
		if (context.GetCurState() != nullptr)
		{
			LOG_BEGIN << "Update State In Machine" << LOG_END;
			context.GetCurState()->OnUpdate(fDeltaT, pAgent);
		}
		else
		{
			context.GetTransition().transferRunRes = OnEnter(pAgent);
		}
	}

	MachineState* FindNextState(MachineState* pCur, MachineState* pFinal)
	{
		if (pCur == pFinal || pFinal == nullptr)
			return nullptr;

		MachineState* pRes;
		MachineState* pPrev = pFinal;
		do 
		{
			pRes = pPrev;
			if (pRes == nullptr)
				break;
			pPrev = (MachineState*)pRes->GetParentMachine()->GetMetaState();
		}
		while (pPrev != pCur);
		
		return pRes;
	}

	bool RootMachine::_Trans(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();

		TransitionContext& transContext = context.GetTransition();

		///> Find the From State
		if (transContext.transferStage <= MTS_None)
		{
			transContext.transferStage = MTS_None;

			if (GetTransition(context.GetCurState(), context, transContext.transferResult))
			{
				///> Found;
			}
			else
			{
				LOG_BEGIN << "Trans Event that cant trans to any states: " << context.GetTransition().Get().GetEvent() << LOG_END;
				return true;
			}
		}

		///> Exit the low level states
		if (transContext.transferStage <= MTS_Exit)
		{
			transContext.transferStage = MTS_Exit;
			while (context.GetCurState() != nullptr && context.GetCurState() != transContext.transferResult.pLCA)
			{
				MachineState* pCurState = context.GetCurState();
				///> The tree not finally return yet
				transContext.transferRunRes = pCurState->OnExit(pAgent);
				if (transContext.transferRunRes != MRR_Normal)
				{
					return false;
				}
				context.PopCurState();
			}

			///>Pop to the target, no need to enter
			if (context.GetCurState() == transContext.transferResult.pToState)
				transContext.transferStage = MTS_Default;
		}

		///> Enter the new state
		if (transContext.transferStage <= MTS_Enter)
		{
			bool failed = false;
			if (transContext.transferStage < MTS_Enter)
			{
				transContext.transferRunRes = MRR_Normal;
					transContext.transferStage = MTS_Enter;
			}
			while (true)
			{
				if (transContext.transferRunRes == MRR_Normal)
				{
					MachineState* pNext = FindNextState(context.GetCurState(), transContext.transferResult.pToState);
					if (!pNext)
					{
						ERROR_BEGIN << "Cant find a route from " << context.GetCurState()->ToString() << " to " << transContext.transferResult.pToState->ToString() << ERROR_END;
						break;
					}
					else
					{
						context.SetCurState(pNext);
					}
				}
				transContext.transferRunRes = context.GetCurState()->OnEnter(pAgent);

				switch (transContext.transferRunRes)
				{
				case YBehavior::MRR_Normal:
					///> Go Next Level
					break;
				case YBehavior::MRR_Exit:
				{
					context.PopCurState();
					failed = true;
				}
				break;
				case YBehavior::MRR_Running:
				case YBehavior::MRR_Break:
					return false;
				default:
					break;
				}
				if (failed)
					break;
				if (context.GetCurState() == transContext.transferResult.pToState)
				{
					///> Meta need to enter the default state
					if (context.GetCurState()->GetType() != MST_Meta)
						return true;
					break;
				}
			}
		}

		///> Enter default state
		if (transContext.transferStage <= MTS_Default)
		{
			transContext.transferStage = MTS_Default;

			while (context.GetCurState() != nullptr)
			{
				MachineState* pCurState = context.GetCurState();
				StateMachine* pCurMachine = nullptr;
				if (transContext.transferRunRes == MRR_Running || transContext.transferRunRes == MRR_Break)
				{
					transContext.transferRunRes = pCurState->OnEnter(pAgent);
				}
				else if (pCurState->GetType() == MST_Meta)
				{
					pCurMachine = static_cast<MetaState*>(pCurState)->GetSubMachine();
					if (!pCurMachine->TryEnterDefault(pAgent))
					{
						///> No Default State, only pop current state, 
						///> instead of both default&submachine in case MRR_Exit below
						transContext.transferRunRes = MRR_Invalid;
						context.PopCurState();
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
					context.PopCurState();

					///> Pop SubMachine
					context.PopCurState();
				default:
					break;
				}
			}

			///> Root machine
			if (m_pDefaultState)
			{
				context.SetCurState(m_pDefaultState);
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
					context.PopCurState();
				default:
					break;
				}
			}
		}

		///> Should not run to here
		transContext.transferRunRes = MRR_Normal;
		return false;
	}

}