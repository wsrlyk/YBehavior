#include "YBehavior/fsm/context.h"
#include "YBehavior/utility.h"
#include "YBehavior/logger.h"
#include "YBehavior/agent.h"
#include "YBehavior/fsm/statemachine.h"
#include "YBehavior/fsm/transition.h"
#include "YBehavior/fsm/behavior.h"

namespace YBehavior
{
	TransitionContext::TransitionContext()
		: m_Trans()
	{

	}

	bool TransitionContext::IncTransCount()
	{
		///> TODO: Check infinite loops, especially when trans has no conditions

		if (++m_TransCount > 10)
		{
			ERROR_BEGIN << "Too many trans in a frame, check your machine." << ERROR_END;
			m_TransCount = 0;
			return false;
		}
		return true;
	}

	MachineContext::MachineContext()
		: m_pCurState (nullptr)
		, m_pCurRunningTree(nullptr)
		, LastRunRes(MRR_Invalid)
	{

	}

	void MachineContext::Init(Behavior* behavior)
	{
		m_Trans.Get().SetConditionMgr(behavior->GetFSM()->GetConditionMgr());
	}

	void MachineContext::Reset()
	{
		m_pCurState = nullptr;
		LastRunRes = MRR_Invalid;
		m_Trans.Reset();
		m_pTransQueue.clear();
		m_pCurRunningTree = nullptr;
	}
}