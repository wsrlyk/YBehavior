#include "YBehavior/nodes/subtree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/behaviortreemgr.h"
#include <string.h>

namespace YBehavior
{
	bool SubTree::OnLoaded(const pugi::xml_node& data)
	{
		//////////////////////////////////////////////////////////////////////////
		TYPEID typeID = CreateVariable(m_TreeName, "Tree", data, Utility::CONST_CHAR);
		if (!m_TreeName)
		{
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

	bool SubTree::OnLoadChild(const pugi::xml_node& data)
	{
		if (strcmp(data.name(), "Input") == 0)
		{
			return _TryCreateFromTo(data, m_Inputs);
		}
		else if (strcmp(data.name(), "Output") == 0)
		{
			return _TryCreateFromTo(data, m_Outputs);
		}
		return true;
	}

	bool SubTree::_TryCreateFromTo(const pugi::xml_node& data, std::vector<ISharedVariableEx*>& container)
	{
		for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
		{
			ISharedVariableEx* pVariable = nullptr;

			CreateVariable(pVariable, it->name(), data, ST_NONE);
			if (!pVariable)
			{
				ERROR_BEGIN << "Failed to Create " << data.name() << ERROR_END;
				return false;
			}
			//if (container.count(it->name()) > 0)
			//{
			//	ERROR_BEGIN << "Duplicate " << data.name() << " Variable: " << it->name() << ERROR_END;
			//	return false;
			//}
			container.push_back(pVariable);
		}

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
			if (m_Inputs.size() > 0 || m_Outputs.size() > 0)
			{
				LocalMemoryInOut inout(pAgent, m_Inputs.size() > 0 ? &m_Inputs : nullptr, m_Outputs.size() > 0 ? &m_Outputs : nullptr);
				return m_Tree->RootExecute(pAgent, m_RunningContext != nullptr ? NS_RUNNING : NS_INVALID, &inout);
			}
			else
			{
				return m_Tree->RootExecute(pAgent, m_RunningContext != nullptr ? NS_RUNNING : NS_INVALID);
			}
		}
		return NS_FAILURE;
	}
}
