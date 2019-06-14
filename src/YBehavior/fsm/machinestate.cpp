#include "YBehavior/fsm/machinestate.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"

namespace YBehavior
{
	MachineState::MachineState(const STRING& name, MachineStateType type, MachineStateCore* pCore, bool bAutoDelCore)
		: m_Name(name)
		, m_Type(type)
		, m_Identification(name)
		, m_pCore(pCore)
		, m_bAutoDelCore(bAutoDelCore)
	{
		m_UID.Value = 0;
	}

	MachineState::MachineState()
		: m_Name("")
		, m_Type(MST_Normal)
		, m_pCore(nullptr)
		, m_bAutoDelCore(false)
	{
		m_UID.Value = 0;
	}

	MachineState::~MachineState()
	{
		if (m_bAutoDelCore && m_pCore)
		{
			delete m_pCore;
			m_pCore = nullptr;
		}
	}

	YBehavior::STRING MachineState::ToString() const
	{
		return m_Name + " " + Utility::ToString(m_UID);
	}

	YBehavior::MachineRunRes MachineState::OnEnter(MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnEnter" << LOG_END;
		if (m_pCore)
			m_pCore->OnEnter(context);
		return MRR_Normal;
	}

	YBehavior::MachineRunRes MachineState::OnExit(MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnExit" << LOG_END;
		if (m_pCore)
			m_pCore->OnExit(context);
		return MRR_Normal;
	}

	void MachineState::OnUpdate(float fDeltaT, MachineContext& context)
	{
		LOG_BEGIN << ToString() << " OnUpdate" << LOG_END;
		if (m_pCore)
			m_pCore->OnUpdate(fDeltaT, context);
	}

}
