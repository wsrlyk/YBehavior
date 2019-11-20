#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/utility.h"
#include "YBehavior/fsm/metastate.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include <list>
#include "YBehavior/fsm/context.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif

namespace YBehavior
{
	StateMachine::StateMachine(UINT uid, UINT level)
		: m_pDefaultState(nullptr)
		, m_EntryState(nullptr)
		, m_ExitState(nullptr)
		, m_pRootMachine(nullptr)
		, m_pMetaState(nullptr)
	{
		m_UID = uid;
		m_Level = level;
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

	bool StateMachine::SetSpecialState(MachineState* pState)
	{
		if (pState == nullptr)
			return false;

		if (pState->GetType() == MST_Entry)
		{
			if (m_EntryState != nullptr)
			{
				ERROR_BEGIN << "Duplicated Entry." << ERROR_END;
				return false;
			}
			m_EntryState = pState;
			m_EntryState->SetUID(m_UID + 1);
			//GetRootMachine()->PushState(pState);
		}
		else if (pState->GetType() == MST_Exit)
		{
			if (m_ExitState != nullptr)
			{
				ERROR_BEGIN << "Duplicated Exit." << ERROR_END;
				return false;
			}
			m_ExitState = pState;
			m_ExitState->SetUID(m_UID + 2);
			//GetRootMachine()->PushState(pState);
		}
		else
		{
			ERROR_BEGIN << "Invalid Type." << ERROR_END;
			return false;
		}
		return true;
	}

	//bool _CompareState(const MachineState* left, const MachineState* right)
	//{
	//	return left->GetSortValue() < right->GetSortValue();
	//}

	void StateMachine::OnLoadFinish()
	{

	}

	void StateMachine::EnterDefaultOrExit(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();
		if (m_pDefaultState)
		{
			context.GetTransQueue().push_back(m_pDefaultState);
		}
		else
		{
			context.GetTransQueue().push_back(m_ExitState);
		}
	}


	FSM::FSM(const STRING& name)
		: m_Version(nullptr)
		, m_pMachine(nullptr)
	{
		m_NameWithPath = name;
		m_Name = Utility::GetNameFromPath(name);
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
		m_pMachine = new RootMachine(0);
		m_pMachine->SetFSM(this);
		return m_pMachine;
	}

	void FSM::Update(float fDeltaT, AgentPtr pAgent)
	{
		if (m_pMachine)
			m_pMachine->Update(fDeltaT, pAgent);

		//MachineContext& context = *pAgent->GetMachineContext();
		//context.GetTransition().Reset();

#ifdef DEBUGGER
		if (DebugMgr::Instance()->GetTargetAgent() == pAgent->GetDebugUID())
		{
			DebugMgr::Instance()->SendInfos(pAgent, true);
		}
#endif
	}


	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////

	RootMachine::RootMachine(UINT uid)
		: StateMachine(uid, 0)
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

		///> Only named states will be inserted into the name map
		if ((pState->GetName() == Utility::StringEmpty || m_NamedStatesMap.insert(std::pair<STRING, MachineState*>(pState->GetName(), pState)).second)
			&&
			m_UIDStatesMap.insert(std::pair<UINT, MachineState*>(pState->GetUID(), pState)).second)
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
		if (k.fromState != nullptr)
		{
			auto res = m_FromTransitionMap.insert(TransitionData(k, v));
			return res.second;
		}
		else
		{
			auto res = m_AnyTransitionMap.insert(TransitionData(k, v));
			return res.second;
		}
	}

	std::list<StateMachine*> _FromRoute;
	std::list<StateMachine*> _ToRoute;
	StateMachine* _FindLCA(MachineState* pFrom, MachineState* pTo)
	{
		if (pFrom == nullptr || pTo == nullptr)
			return nullptr;

		_FromRoute.clear();
		_ToRoute.clear();

		StateMachine* pDeeper;
		StateMachine* pShallower;

		std::list<StateMachine*>* pDeeperRoute;
		std::list<StateMachine*>* pShallowerRoute;

		bool bFromIsDeeper = false;
		if (pFrom->GetParentMachine()->GetLevel() > pTo->GetParentMachine()->GetLevel())
		{
			bFromIsDeeper = true;
			pDeeper = pFrom->GetParentMachine();
			pShallower = pTo->GetParentMachine();

			pDeeperRoute = &_FromRoute;
			pShallowerRoute = &_ToRoute;
		}
		else
		{
			pDeeper = pTo->GetParentMachine();
			pShallower = pFrom->GetParentMachine();

			pDeeperRoute = &_ToRoute;
			pShallowerRoute = &_FromRoute;
		}

		for (UINT i = pDeeper->GetLevel() - pShallower->GetLevel(); i > 0; --i)
		{
			pDeeperRoute->push_back(pDeeper);
			pDeeper = pDeeper->GetParentMachine();
		}

		///> the Ancestor will be in neither of these two lists
		while (pDeeper != pShallower && pDeeper != nullptr)
		{
			pDeeperRoute->push_back(pDeeper);
			pShallowerRoute->push_back(pShallower);

			pDeeper = pDeeper->GetParentMachine();
			pShallower = pShallower->GetParentMachine();
		}

		return pDeeper;
	}

	void _MakeLCARoute(std::list<MachineState*>& route)
	{
		route.clear();

		while (!_FromRoute.empty())
		{
			StateMachine* d = _FromRoute.front();
			_FromRoute.pop_front();
			if (d != nullptr)
				route.push_back(d->GetExit());
		}

		while (!_ToRoute.empty())
		{
			StateMachine* d = _ToRoute.back();
			_ToRoute.pop_back();
			if (d != nullptr)
				route.push_back(d->GetEntry());
		}
	}

	bool RootMachine::GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result)
	{
		TransitionMapKey key;
		if (pCurState != nullptr)
		{
			///> First find CurState->XXX
			key.fromState = pCurState;
			key.trans = context.GetTransition().Get();
			auto it = std::find_if(m_FromTransitionMap.begin(), m_FromTransitionMap.end(), CanTransTeller(key));
			if (it == m_FromTransitionMap.end())
			{
				///> Then find AnyState->XXX
				key.fromState = nullptr;
				it = std::find_if(m_AnyTransitionMap.begin(), m_AnyTransitionMap.end(), CanTransTeller(key));
				if (it == m_AnyTransitionMap.end())
					return false;
			}

			if (it->Value.toState != pCurState /*Cant Self -> Self through Any*/)
			{
				result.pFromState = key.fromState;
				result.trans = key.trans;
				result.pToState = it->Value.toState;
				result.pMachine = this;
				
				_FindLCA(pCurState, result.pToState);
				_MakeLCARoute(result.lcaRoute);
				return true;
			}
		}
		return false;
	}

	YBehavior::MachineState* RootMachine::FindState(const STRING& name)
	{
		auto it = m_NamedStatesMap.find(name);
		if (it == m_NamedStatesMap.end())
			return nullptr;
		return it->second;
	}

	YBehavior::MachineState* RootMachine::FindState(UINT uid)
	{
		auto it = m_UIDStatesMap.find(uid);
		if (it == m_UIDStatesMap.end())
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

		if (context.GetCurState() == nullptr)
			context.GetTransQueue().push_back(m_EntryState);
		else if (context.LastRunRes == MRR_Running || context.LastRunRes == MRR_Break)
		{
			context.LastRunRes = context.GetCurState()->Execute(pAgent, context.LastRunRes);
			if (context.LastRunRes == MRR_Running || context.LastRunRes == MRR_Break)
				return;
		}

		if (context.GetTransQueue().size() == 0)
		{
			///> No Trans, Just Update Current
			if (!_Trans(pAgent))
			{
				context.LastRunRes = context.GetCurState()->Execute(pAgent, context.LastRunRes);
			}

		}
		while (context.GetTransQueue().size() > 0)
		{
			while (context.GetTransQueue().size() > 0)
			{
				if (!(context.LastRunRes == MRR_Running || context.LastRunRes == MRR_Break))
				{
					MachineState* pNextState = context.GetTransQueue().front().pState;
					context.GetTransQueue().pop_front();
					context.SetCurState(pNextState);
				}

				context.LastRunRes = context.GetCurState()->Execute(pAgent, context.LastRunRes);
				if (context.LastRunRes == MRR_Running || context.LastRunRes == MRR_Break)
					return;
			}

			///> Find a trans, push all states in the trans into the queue
			if (_Trans(pAgent))
			{
				continue;
			}

			///> Trans to an Entry, and this Entry has no trans to other states, then just trans to Default
			if (context.GetCurState()->GetType() == MST_Entry)
			{
				context.GetCurState()->GetParentMachine()->EnterDefaultOrExit(pAgent);
			}
			else if (context.GetCurState()->GetType() == MST_Exit)
			{
				///> Is RootMachine, Just exit
				if (context.GetCurState()->GetParentMachine()->GetMetaState() == nullptr)
				{
					context.SetCurState(nullptr);
				}
				///> Go to ParentMachine's Default
				else
				{
					context.GetCurState()->GetParentMachine()->GetMetaState()->GetParentMachine()->EnterDefaultOrExit(pAgent);
				}
			}
		}
	}

	bool RootMachine::_Trans(AgentPtr pAgent)
	{
		MachineContext& context = *pAgent->GetMachineContext();

		TransitionContext& transContext = context.GetTransition();

		///> Find the From State
		if (GetTransition(context.GetCurState(), context, transContext.transferResult))
		{
			///> Found;
			///> To avoid trans to destination again and again, clear its condition once we get a trans
			context.GetTransition().Get().UnSet(transContext.transferResult.trans);

			///> too many
			if (!transContext.IncTransCount())
				return false;
		}
		else
		{
			return false;
		}

		///> Exit->Exit->...(AncesterMachine)...->Entry->Entry->Target
		while (!transContext.transferResult.lcaRoute.empty())
		{
			MachineState* pState = transContext.transferResult.lcaRoute.front();
			transContext.transferResult.lcaRoute.pop_front();
			context.GetTransQueue().push_back(pState);
		}
		context.GetTransQueue().push_back(transContext.transferResult.pToState);

		return true;
	}

}