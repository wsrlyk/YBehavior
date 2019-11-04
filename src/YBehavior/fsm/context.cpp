#include "YBehavior/fsm/context.h"
#include "YBehavior/utility.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/fsm/transition.h"
#include "YBehavior/fsm/machinetreemapping.h"

namespace YBehavior
{
	TransitionContext::TransitionContext()
		: m_Trans()
		, m_bLock(false)
	{

	}

	bool TransitionContext::IncTransCount()
	{
		///> TODO: Check infinite loops, especially when trans has no conditions

		if (++m_TransCount > 10)
		{
			ERROR_BEGIN << "Too many trans in a frame, check your machine." << ERROR_END;
			return false;
		}
		return true;
	}

	MachineContext::MachineContext()
		: m_pCurState (nullptr)
		, m_pMapping(nullptr)
		, LastRunRes(MRR_Invalid)
	{

	}

	void MachineContext::SetMapping(MachineTreeMapping* mapping)
	{
		m_pMapping = mapping;
		m_Trans.Get().SetConditionMgr(m_pMapping->GetFSM()->GetConditionMgr());
	}

	void MachineContext::Reset()
	{
		m_pMapping = nullptr;
		m_pCurState = nullptr;
		m_Trans.Reset();
	}
}