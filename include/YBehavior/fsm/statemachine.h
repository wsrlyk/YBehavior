#ifndef _YBEHAVIOR_STATEMACHINE_H_
#define _YBEHAVIOR_STATEMACHINE_H_

#include "YBehavior/types.h"
#include "machinestate.h"
#include <map>
#include <list>
#include "YBehavior/utility.h"
#include <unordered_map>

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
		MachineState* pLCA;
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
		inline void Reset() { m_Trans.Reset(); m_bLock = false; transferStage = MTS_None; }

		inline void Lock() { m_bLock = true; }
	};

	//typedef std::list<MachineState*> CurrentStatesType;
	class MachineTreeMapping;
	class BehaviorTree;
	class MachineContext
	{
	protected:
		//CurrentStatesType m_CurStates;
		MachineState* m_pCurState;
		TransitionContext m_Trans;
		MachineTreeMapping* m_pMapping;
		MachineState* m_pCurRunningState;
		BehaviorTree* m_pCurRunningTree;
	public:
		MachineContext();
		inline TransitionContext& GetTransition() { return m_Trans; }
		inline const TransitionContext& GetTransition() const { return m_Trans; }
		//inline CurrentStatesType& GetCurStatesStack() { return m_CurStates; }
		inline void SetMapping(MachineTreeMapping* mapping) { m_pMapping = mapping; }
		inline MachineTreeMapping* GetMapping() { return m_pMapping; }
		inline void ResetCurRunning() { m_pCurRunningState = nullptr; m_pCurRunningTree = nullptr; }
		inline void SetCurRunning(MachineState* pCurRunningState, BehaviorTree* pCurRunningTree) { m_pCurRunningState = pCurRunningState; m_pCurRunningTree = pCurRunningTree; }
		inline MachineState* GetCurRunningState() { return m_pCurRunningState; }
		inline BehaviorTree* GetCurRunningTree() { return m_pCurRunningTree; }
		inline bool CanRun(MachineState* pState) { return m_pCurRunningState == nullptr || m_pCurRunningState == pState; }
		inline void SetCurState(MachineState* pState) { m_pCurState = pState; }
		inline MachineState* GetCurState() const { return m_pCurState; }
		void Reset();
		void PopCurState();
	};

	class RootMachine;
	class MetaState;
	class StateMachine
	{
	protected:
		MachineState* m_pDefaultState;
		MachineState* m_EntryState;
		MachineState* m_ExitState;
		FSMUID m_UID;
		RootMachine* m_pRootMachine;
		MetaState* m_pMetaState;
	public:
		StateMachine(FSMUIDType layer, FSMUIDType level, FSMUIDType index);
		~StateMachine();
		inline MachineState* GetEntry() { return m_EntryState; }
		inline MachineState* GetExit() { return m_ExitState; }
		inline FSMUID GetUID() const { return m_UID; }
		void SetMetaState(MetaState* pState);
		inline MetaState* GetMetaState() const { return m_pMetaState; }
		inline RootMachine* GetRootMachine() const { return m_pRootMachine; }
		StateMachine* GetParentMachine() const;
		inline void SetDefault(MachineState* pState) { m_pDefaultState = pState; }
		void SetSpecialState(MachineState* pState);

		virtual void OnLoadFinish();

		MachineRunRes OnEnter(AgentPtr pAgent);
		MachineRunRes OnExit(AgentPtr pAgent);
		bool TryEnterDefault(AgentPtr pAgent);
	protected:
	};

	class RootMachine : public StateMachine
	{
	protected:
		std::map<TransitionMapKey, TransitionMapValue> m_TransitionMap;
		std::unordered_map<STRING, MachineState*> m_States;
		std::vector<MachineState*> m_AllStates;
	public:
		RootMachine(FSMUIDType layer);
		~RootMachine();
		bool InsertState(MachineState* pState);
		void PushState(MachineState* pState);
		bool InsertTrans(const TransitionMapKey&, const TransitionMapValue&);
		inline std::vector<MachineState*>& GetAllStates() { return m_AllStates; }
		bool GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result);
		MachineState* FindState(const STRING& name);
		void Update(float fDeltaT, AgentPtr pAgent);

		void OnLoadFinish() override;
	protected:
		bool _Trans(AgentPtr pAgent);
	};

	struct MachineVersion;
	class MachineID;
	class FSM
	{
		STRING m_Name;
		MachineVersion* m_Version;
		MachineID* m_ID;
		///> TODO: multilayers
		RootMachine* m_pMachine;
	public:
		inline void SetVersion(MachineVersion* v) { m_Version = v; }
		inline MachineVersion* GetVersion() const { return m_Version; }
		inline void SetID(MachineID* id) { m_ID = id; }
		inline MachineID* GetID() const { return m_ID; }
		inline RootMachine* GetMachine() { return m_pMachine; }

		FSM(const STRING& name);
		~FSM();
		RootMachine* CreateMachine();

		void Update(float fDeltaT, AgentPtr pAgent);
	};
}

#endif