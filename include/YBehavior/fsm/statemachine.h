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
		Transition() {}
		Transition(const STRING& e) : m_Event(e) {}
		inline bool operator==(const Transition& _rhs) const { return m_Event == _rhs.m_Event; }
		inline bool operator<(const Transition& _rhs) const { return m_Event < _rhs.m_Event; }
		inline const std::string GetEvent() const { return m_Event; }
		inline bool IsValid() const { return m_Event != Utility::StringEmpty; }
		inline void Reset() { m_Event = Utility::StringEmpty; }
		inline void Set(const STRING& e) { m_Event = e; }
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

	class StateMachine;
	struct TransitionResult
	{
		MachineState* pFromState;
		MachineState* pToState;
		Transition trans;
		StateMachine* pMachine;

		TransitionResult()
			: pFromState(nullptr)
			, pToState(nullptr)
			, pMachine(nullptr)
		{
		}
	};

	enum MachineTransferStage
	{
		MTS_None,
		MTS_Exit,
		MTS_Enter,
		MTS_Default,
	};

	class TransitionContext
	{
		Transition m_Trans;
		bool m_bLock;

	public:
		MachineTransferStage transferStage;
		TransitionResult transferResult;
		MachineRunRes transferRunRes;
	public:
		TransitionContext();
		TransitionContext(const STRING& e);
		const Transition& Get() const { return m_Trans; }
		bool HasTransition() { return m_Trans.IsValid(); }
		void Set(const STRING& e) { if (!m_bLock) m_Trans.Set(e); }
		inline void Reset() { m_Trans.Reset(); m_bLock = false; }

		inline void Lock() { m_bLock = true; }
	};

	typedef std::list<MachineState*> CurrentStatesType;
	class MachineTreeMapping;
	class MachineContext
	{
	protected:
		CurrentStatesType m_CurStates;
		TransitionContext m_Trans;
		MachineTreeMapping* m_pMapping;
		MachineState* m_pCurRunningState;
	public:
		MachineContext();
		inline TransitionContext& GetTransition() { return m_Trans; }
		inline const TransitionContext& GetTransition() const { return m_Trans; }
		inline CurrentStatesType& GetCurStatesStack() { return m_CurStates; }
		inline void SetMapping(MachineTreeMapping* mapping) { m_pMapping = mapping; }
		inline MachineTreeMapping* GetMapping() { return m_pMapping; }
		inline void ResetCurRunningState() { m_pCurRunningState = nullptr; }
		inline void SetCurRunningState(MachineState* pCurRunningState) { m_pCurRunningState = pCurRunningState; }
		inline MachineState* GetCurRunningState() { return m_pCurRunningState; }
		inline bool CanRun(MachineState* pState) { return m_pCurRunningState == nullptr || m_pCurRunningState == pState; }
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
		void Update(float fDeltaT, AgentPtr pAgent);

		void OnLoadFinish();

		MachineRunRes OnEnter(AgentPtr pAgent);
		MachineRunRes OnExit(AgentPtr pAgent);
	protected:
		bool _Trans(CurrentStatesType::const_iterator it, AgentPtr pAgent, TransitionResult& res);
		bool _Trans(AgentPtr pAgent);
		bool _TryEnterDefault(AgentPtr pAgent);
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