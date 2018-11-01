#include "YBehavior/nodes/wait.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER

namespace YBehavior
{
	bool Wait::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		TYPEID type = CreateVariable(m_TickCount, "TickCount", data, ST_SINGLE);
		if (!m_TickCount)
		{
			return false;
		}

		return true;
	}

	YBehavior::NodeState Wait::Update(AgentPtr pAgent)
	{
		IF_HAS_LOG_POINT
		{
			LOG_SHARED_DATA(m_TickCount, true);
		}

		INT tickCount = 0;
		m_TickCount->GetCastedValue(pAgent->GetMemory(), tickCount);
		m_RCContainer.CreateRC(this);

		if (++m_RCContainer.GetRC()->Current > tickCount)
			return NS_SUCCESS;
		DEBUG_LOG_INFO("Tick " << m_RCContainer.GetRC()->Current << "; ");
		return NS_RUNNING;
	}
}
