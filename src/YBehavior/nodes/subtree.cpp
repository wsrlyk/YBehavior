#include "YBehavior/nodes/subtree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER
#include "YBehavior/behaviortreemgr.h"

namespace YBehavior
{
	void SubTree::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		TYPEID typeID = CreateVariable(m_TreeName, "Tree", data, true, Utility::CONST_CHAR);
		if (typeID != GetClassTypeNumberId<STRING>())
		{
			ERROR_BEGIN << "Invalid type for Tree in SubTree: " << typeID << ERROR_END;
			return;
		}

		const STRING* treeName = (const STRING*)m_TreeName->GetCastedValue(nullptr);
		if (treeName == nullptr || *treeName == Utility::StringEmpty)
		{
			ERROR_BEGIN << "Null Value for Tree in SubTree: " << typeID << ERROR_END;
			return;
		}

		TreeMgr::Instance()->PushToBeLoadedTree(*treeName);
	}

	YBehavior::NodeState SubTree::Update(AgentPtr pAgent)
	{
		if (m_Tree == nullptr)
		{

		}

		return NS_SUCCESS;
	}
}
