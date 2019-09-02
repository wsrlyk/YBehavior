#ifndef _YBEHAVIOR_STATEMACHINE_H_
#define _YBEHAVIOR_STATEMACHINE_H_

#include "YBehavior/types.h"
#include "machinestate.h"
#include <set>
#include <list>
#include "YBehavior/utility.h"
#include <unordered_map>
#include "transition.h"

namespace YBehavior
{
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
		bool SetSpecialState(MachineState* pState);

		virtual void OnLoadFinish();

		void EnterDefaultOrExit(AgentPtr pAgent);
	protected:
	};

	class FSM;
	class RootMachine : public StateMachine
	{
	protected:
		std::set<TransitionData> m_FromTransitionMap;
		std::set<TransitionData> m_AnyTransitionMap;

		std::unordered_map<STRING, MachineState*> m_NamedStatesMap;
		std::unordered_map<FSMUIDType, MachineState*> m_UIDStatesMap;
		std::vector<MachineState*> m_AllStates;
		FSM* m_pFSM;
	public:
		RootMachine(FSMUIDType layer);
		~RootMachine();
		inline FSM* GetFSM() { return m_pFSM; }
		inline void SetFSM(FSM* pFSM) { m_pFSM = pFSM; }
		bool InsertState(MachineState* pState);
		void PushState(MachineState* pState);
		bool InsertTrans(const TransitionMapKey&, const TransitionMapValue&);
		inline std::vector<MachineState*>& GetAllStates() { return m_AllStates; }
		bool GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result);
		MachineState* FindState(FSMUIDType uid);
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
		ConditionMgr m_ConditionMgr;
	public:
		inline void SetVersion(MachineVersion* v) { m_Version = v; }
		inline MachineVersion* GetVersion() const { return m_Version; }
		inline void SetID(MachineID* id) { m_ID = id; }
		inline MachineID* GetID() const { return m_ID; }
		inline RootMachine* GetMachine() { return m_pMachine; }
		inline ConditionMgr* GetConditionMgr() { return &m_ConditionMgr; }
		FSM(const STRING& name);
		~FSM();
		RootMachine* CreateMachine();

		void Update(float fDeltaT, AgentPtr pAgent);
	};
}

#endif