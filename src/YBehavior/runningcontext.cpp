#include "YBehavior/runningcontext.h"
#include "YBehavior/agent.h"

void YBehavior::RunningContext::Reset()
{
	m_UID = 0;
	m_bRunningInCondition = false;

	_OnReset();
}

void YBehavior::VectorTraversalContext::_OnReset()
{
	Current = 0;
}

void YBehavior::RandomVectorTraversalContext::_OnReset()
{
	VectorTraversalContext::_OnReset();
	IndexList.clear();
}

