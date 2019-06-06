#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"

namespace YBehavior
{
	MachineState::MachineState(const STRING& name, MachineStateType type)
		: m_Name(name)
		, m_Type(type)
		, m_UID(0)
	{

	}

	MachineState::MachineState()
		: m_Name("")
		, m_Type(MST_Normal)
		, m_UID(0)
	{

	}

	YBehavior::STRING MachineState::ToString() const
	{
		return m_Name + " " + Utility::ToString(m_UID);
	}

	YBehavior::MachineRunRes MachineState::OnEnter(MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnEnter" << LOG_END;
		return MRR_Normal;
	}

	YBehavior::MachineRunRes MachineState::OnExit(MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnExit" << LOG_END;
		return MRR_Normal;
	}

	void MachineState::OnUpdate(float fDeltaT, MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnUpdate" << LOG_END;
	}

}
