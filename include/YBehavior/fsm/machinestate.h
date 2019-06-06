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
	class MachineState
	{
	protected:
		STRING m_Name;
		MachineStateType m_Type;
		UINT m_UID;
	public:
		MachineState();
		MachineState(const STRING& name, MachineStateType type);
		inline const STRING& GetName() const { return m_Name; }
		inline MachineStateType GetType() const { return m_Type; }
		virtual STRING ToString() const;
		virtual MachineRunRes OnEnter(MachineContext& context);
		virtual MachineRunRes OnExit(MachineContext& context);
		virtual void OnUpdate(float fDeltaT, MachineContext& context);
	};
}

#endif