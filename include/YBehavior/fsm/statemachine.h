#ifndef _YBEHAVIOR_STATEMACHINE_H_
#define _YBEHAVIOR_STATEMACHINE_H_

#include "YBehavior/types/types.h"
#include <set>
#include "YBehavior/types/smallmap.h"
#include "transition.h"
#include "YBehavior/types/treemap.h"

namespace YBehavior
{
	class RootMachine;
	class MetaState;
	class MachineState;
	class MachineContext;
	class StateMachine
	{
	protected:
		MachineState* m_pDefaultState;
		MachineState* m_EntryState;
		MachineState* m_ExitState;
		UINT m_Level;
		UINT m_UID;	///> Same with the meta state.

		RootMachine* m_pRootMachine;
		MetaState* m_pMetaState;
	public:
		StateMachine(UINT uid, UINT level);
		virtual ~StateMachine();
		inline MachineState* GetEntry() { return m_EntryState; }
		inline MachineState* GetExit() { return m_ExitState; }
		inline UINT GetLevel() const { return m_Level; }
		inline void SetLevel(UINT level) { m_Level = level; }
		virtual UINT GetUIDOffset() const { return 4; }///> Entry, Exit, Any, Upper;    to keep step with Editor
		void SetMetaState(MetaState* pState);
		inline MetaState* GetMetaState() const { return m_pMetaState; }
		inline RootMachine* GetRootMachine() const { return m_pRootMachine; }
		StateMachine* GetParentMachine() const;
		inline void SetDefault(MachineState* pState) { m_pDefaultState = pState; }
		bool SetSpecialState(MachineState* pState, UINT uid);

		virtual void OnLoadFinish();

		void EnterDefaultOrExit(AgentPtr pAgent);
		void EnterEntry(AgentPtr pAgent);
	protected:
	};

	class FSM;
	class RootMachine : public StateMachine
	{
	protected:
		std::set<TransitionData> m_FromTransitionMap;
		std::set<TransitionData> m_AnyTransitionMap;

		small_map<STRING, MachineState*> m_NamedStatesMap;
		small_map<UINT, MachineState*> m_UIDStatesMap;
		std::vector<MachineState*> m_AllStates;
		FSM* m_pFSM;
	public:
		RootMachine(UINT uid);
		~RootMachine();
		inline FSM* GetFSM() { return m_pFSM; }
		inline void SetFSM(FSM* pFSM) { m_pFSM = pFSM; }
		bool InsertState(MachineState* pState);
		void PushState(MachineState* pState);
		bool InsertTrans(const TransitionMapKey&, const TransitionMapValue&);
		inline std::vector<MachineState*>& GetAllStates() { return m_AllStates; }
		bool GetTransition(MachineState* pCurState, const MachineContext& context, TransitionResult& result);
		MachineState* FindState(UINT uid);
		MachineState* FindState(const STRING& name);
		void Update(float fDeltaT, AgentPtr pAgent);

		void OnLoadFinish() override;
		UINT GetUIDOffset() const override { return 3; }///> Entry, Exit, Any;   No Upper

	protected:
		bool _Trans(AgentPtr pAgent);
	};

	class MachineID;
	class FSM
	{
		STRING m_Name;
		STRING m_NameWithPath;
		void* m_Version;
		MachineID* m_ID;
		///> TODO: multilayers
		RootMachine* m_pMachine;
		ConditionMgr m_ConditionMgr;
		TreeMap m_TreeMap;

#ifdef YDEBUGGER
		UINT m_Hash;
#endif
	public:
		inline void SetVersion(void* v) { m_Version = v; }
		inline void* GetVersion() const { return m_Version; }
		inline void SetID(MachineID* id) { m_ID = id; }
		inline MachineID* GetID() const { return m_ID; }
		inline RootMachine* GetMachine() { return m_pMachine; }
		inline ConditionMgr* GetConditionMgr() { return &m_ConditionMgr; }
		inline const STRING& GetName() { return m_Name; }
		inline const STRING& GetFullName() { return m_NameWithPath; }
		inline const STRING& GetKey() { return m_NameWithPath; }
		inline TreeMap& GetTreeMap() { return m_TreeMap; }

		FSM(const STRING& name);
		~FSM();
		RootMachine* CreateMachine();

		void Update(float fDeltaT, AgentPtr pAgent);
#ifdef YDEBUGGER
		inline UINT GetHash() { return m_Hash; }
		inline void SetHash(UINT hash) { m_Hash = hash; }
#endif
	};
}

#endif