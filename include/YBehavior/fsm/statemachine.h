#ifndef _YBEHAVIOR_STATEMACHINE_H_
#define _YBEHAVIOR_STATEMACHINE_H_

#include "YBehavior/types.h"
#include "machinestate.h"
#include <map>
#include <list>
#include "YBehavior/utility.h"
#include <unordered_set>

namespace YBehavior
{
	class Transition
	{
	protected:
		STRING m_Event;
	public:
		Transition(){}
		Transition(const STRING& e) : m_Event(e){}
		inline bool operator==(const Transition& _rhs) const { return m_Event == _rhs.m_Event; }
		inline bool operator<(const Transition& _rhs) const { return m_Event < _rhs.m_Event; }
		inline const std::string GetEvent() const { return m_Event; }
		inline bool IsValid() const { return m_Event != Utility::StringEmpty; }
		inline void Reset() { m_Event = Utility::StringEmpty; }
		inline void Set(const STRING& e) { m_Event = e; }

	};

	typedef std::list<MachineState*> CurrentStateType;
	class MachineContext
	{
	protected:
		CurrentStateType m_CurState;
		Transition m_Trans;
	public:
		inline Transition& GetTransition() { return m_Trans; }
		inline const Transition& GetTransition() const { return m_Trans; }
		inline CurrentStateType& GetCurStateStack() { return m_CurState; }
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

	struct TransitionResult
	{
		MachineState* pFromState;
		MachineState* pToState;
		Transition trans;
		
		TransitionResult()
			: pFromState(nullptr)
			, pToState(nullptr)
		{
		}
	};

	class StateMachine
	{
	protected:
		std::map<TransitionMapKey, TransitionMapValue> m_TransitionMap;
		std::unordered_set<MachineState*> m_States;
		std::vector<MachineState*> m_AllStates;
		MachineState* m_pDefaultState;
		MachineState* m_EntryState;
		MachineState* m_ExitState;
		FSMUID m_UID;
	public:
		StateMachine(FSMUIDType layer, FSMUIDType level, FSMUIDType index);
		~StateMachine();
		void InsertTrans(const TransitionMapKey&, const TransitionMapValue&);
		bool GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result);
		inline MachineState* GetEntry() { return m_EntryState; }
		inline MachineState* GetExit() { return m_ExitState; }
		inline FSMUID GetUID() const { return m_UID; }
		inline std::vector<MachineState*>& GetAllStates() { return m_AllStates; }
		inline void SetDefault(MachineState* pState) { m_pDefaultState = pState; }
		void SetSpecialState(MachineState* pState);

		void CheckDefault(MachineContext& context);
		void Update(float fDeltaT, MachineContext& context);

		void OnLoadFinish();

		MachineRunRes OnEnter(MachineContext& context);
		MachineRunRes OnExit(MachineContext& context);
	protected:
		bool _Trans(CurrentStateType::const_iterator it, MachineContext& context, TransitionResult& res);
		bool _TryEnterDefault(MachineContext& context);
	};

	struct MachineVersion;
	class BehaviorID;
	class FSM
	{
		STRING m_Name;
		MachineVersion* m_Version;
		///> TODO: multilayers
		StateMachine* m_pMachine;
	public:
		inline void SetVersion(MachineVersion* v) { m_Version = v; }
		inline MachineVersion* GetVersion() const { return m_Version; }
		inline StateMachine* GetMachine() { return m_pMachine; }

		FSM(const STRING& name);
		~FSM();
		StateMachine* CreateMachine();
	};
}

#endif