#include "YBehavior/nodes/wait.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/variablecreation.h"

namespace YBehavior
{
	bool Wait::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		VariableCreation::CreateVariable(this, m_TickCount, "TickCount", data);
		if (!m_TickCount)
		{
			return false;
		}

		return true;
	}

	void WaitNodeContext::_OnInit()
	{
		TreeNodeContext::_OnInit();
		m_Count = 0;
	}

	NodeState WaitNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		Wait* pNode = (Wait*)m_pNode;
		INT tickCount = 0;
		pNode->m_TickCount->GetCastedValue(pAgent->GetMemory(), tickCount);
		YB_LOG_INFO(".." << m_Count);
		if (++m_Count > tickCount)
			return NS_SUCCESS;
		
		return NS_RUNNING;
	}

}
