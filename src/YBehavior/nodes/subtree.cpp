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
	bool SubTree::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		TYPEID typeID = CreateVariable(m_TreeName, "Tree", data, true, Utility::CONST_CHAR);
		if (typeID != GetClassTypeNumberId<STRING>())
		{
			ERROR_BEGIN << "Invalid type for Tree in SubTree: " << typeID << ERROR_END;
			return false;
		}

		STRING treeName;
		m_TreeName->GetCastedValue(nullptr, treeName);
		if (treeName == Utility::StringEmpty)
		{
			ERROR_BEGIN << "Null Value for Tree in SubTree: " << typeID << ERROR_END;
			return false;
		}

		if (m_Root == nullptr || m_Root->GetTreeNameWithPath() != treeName)
			TreeMgr::Instance()->PushToBeLoadedTree(treeName);

		return true;
	}

	YBehavior::NodeState SubTree::Update(AgentPtr pAgent)
	{
		if (m_Tree == nullptr && m_Root != nullptr)
		{
			STRING treeName;
			m_TreeName->GetCastedValue(nullptr, treeName);
			for (auto it = m_Root->GetSubTrees().begin(); it != m_Root->GetSubTrees().end(); ++it)
			{
				if ((*it)->GetTreeNameWithPath() == treeName)
				{
					m_Tree = *it;
					break;
				}
			}
			if (m_Tree == nullptr && m_Root->GetTreeNameWithPath() == treeName)
				m_Tree = m_Root;
		}

		if (m_Tree != nullptr)
		{
			return m_Tree->Execute(pAgent, m_RunningContext != nullptr ? NS_RUNNING : NS_INVALID);
		}
		return NS_FAILURE;
	}
}
