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
		MRR_Normal,
		MRR_Exit,
	};
	class MachineContext;

	class MachineStateCore
	{
	public:
		virtual void OnEnter(MachineContext& context) {}
		virtual void OnExit(MachineContext& context) {}
		virtual void OnUpdate(float fDeltaT, MachineContext& context) {}
	};

	class MachineState
	{
	protected:
		STRING m_Name;
		STRING m_Tree;
		STRING m_Identification;

		MachineStateType m_Type;
		FSMUID m_UID;
		MachineStateCore* m_pCore;
		bool m_bAutoDelCore;
	public:
		MachineState();
		MachineState(const STRING& name, MachineStateType type, MachineStateCore* pCore, bool bAutoDelCore = true);
		~MachineState();
		inline const STRING& GetName() const { return m_Name; }
		inline const STRING& GetIdentification() const { return m_Identification; }
		inline void SetIdentification(const STRING& id) { m_Identification = id; }
		inline FSMUID& GetUID() { return m_UID; }
		inline MachineStateType GetType() const { return m_Type; }
		virtual STRING ToString() const;
		virtual MachineRunRes OnEnter(MachineContext& context);
		virtual MachineRunRes OnExit(MachineContext& context);
		virtual void OnUpdate(float fDeltaT, MachineContext& context);
		inline void SetTree(const STRING& tree) { m_Tree = tree; }
		inline const STRING& GetTree() { return m_Tree; }
	};
}

#endif