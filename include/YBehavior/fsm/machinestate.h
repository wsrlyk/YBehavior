#ifndef _YBEHAVIOR_MACHINESTATE_H_
#define _YBEHAVIOR_MACHINESTATE_H_

#include "YBehavior/types.h"

namespace YBehavior
{
	enum MachineStateType
	{
		MST_Entry,
		MST_Exit,
		MST_Any,
		MST_Meta,
		MST_Normal,
	};

	enum MachineRunRes
	{
		MRR_Invalid = -1,
		MRR_Normal = 0,
		MRR_Exit =1,
		MRR_Break = 2,
		MRR_Running = 3,
	};
	class MachineContext;

	class MachineStateCore
	{
	public:
		virtual void OnEnter(AgentPtr pAgent) {}
		virtual void OnExit(AgentPtr pAgent) {}
		virtual void OnUpdate(float fDeltaT, AgentPtr pAgent) {}
	};

	class StateMachine;
	class DebugFSMHelper;
	class MachineState
	{
	protected:
		STRING m_Name;
		STRING m_Tree;

		MachineStateType m_Type;
		UINT m_UID;

		//int m_SortValue;
		StateMachine* m_pParentMachine;
#ifdef YDEBUGGER
	protected:
		std::stringstream m_DebugLogInfo;
		DebugFSMHelper* m_pDebugHelper;
		bool _HasLogPoint();
#define IF_HAS_LOG_POINT if (_HasLogPoint())
#define DEBUG_LOG_INFO(info)\
	{\
		IF_HAS_LOG_POINT\
			m_DebugLogInfo << info;\
	}
	public:
		std::stringstream& GetDebugLogInfo() { return m_DebugLogInfo; }
#else
#define DEBUG_LOG_INFO(info);
#define IF_HAS_LOG_POINT
#endif 
	public:
		MachineState();
		MachineState(const STRING& name, MachineStateType type);
		virtual ~MachineState();
		inline const STRING& GetName() const { return m_Name; }
		inline void SetName(const STRING& name) { m_Name = name; }
		inline UINT GetUID() const { return m_UID; }
		inline void SetUID(UINT uid) { m_UID = uid; }
		inline void SetParentMachine(StateMachine* pMac) { m_pParentMachine = pMac; }
		inline StateMachine* GetParentMachine() const { return m_pParentMachine; }
		inline MachineStateType GetType() const { return m_Type; }
		virtual STRING ToString() const;
		MachineRunRes Execute(AgentPtr pAgent, MachineRunRes previousState);
		inline void SetTree(const STRING& tree) { m_Tree = tree; }
		inline const STRING& GetTree() { return m_Tree; }
		//inline int GetSortValue() const { return m_SortValue; }
		//inline void SetSortValue(int v) { m_SortValue = v; }

	protected:
		MachineRunRes _OnUpdate(AgentPtr pAgent);
		MachineRunRes _RunTree(AgentPtr pAgent);
	};
}

#endif