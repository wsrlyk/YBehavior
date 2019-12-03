#include "YBehavior/nodes/subtree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/behaviortreemgr.h"
#include <string.h>
#include "YBehavior/mgrs.h"
#include "YBehavior/fsm/behavior.h"

namespace YBehavior
{
	bool SubTree::OnLoaded(const pugi::xml_node& data)
	{
		if (!m_Root)
		{
			ERROR_BEGIN << "No Root" << ERROR_END;
			return false;
		}

		CreateVariable(m_Identification, "Identification", data, Utility::CONST_CHAR);
		if (!m_Identification)
		{
			return false;
		}
		
		CreateVariable(m_TreeName, "Tree", data, Utility::CONST_CHAR);
		if (!m_TreeName)
		{
			return false;
		}

		STRING defaultTreeName;
		m_TreeName->GetCastedValue(nullptr, defaultTreeName);
		STRING id;
		m_Identification->GetCastedValue(nullptr, id);

		/*m_Root->GetTreeID()->TryGet(id, defaultTreeName, m_FinalTreeName);
		
		if (m_FinalTreeName == Utility::StringEmpty)
		{
			ERROR_BEGIN << "Null Value for Tree in " << this->GetClassName() << ERROR_END;
			return false;
		}

		if (m_Root->GetTreeNameWithPath() != m_FinalTreeName)
			Mgrs::Instance()->GetTreeMgr()->PushToBeLoadedTree(m_FinalTreeName);*/
		if (id.empty())
		{
			if (defaultTreeName.empty())
			{
				ERROR_BEGIN << "Null Value for Tree in " << this->GetClassName() << ERROR_END;
				return false;
			}
			m_Root->GetTreeMap().Node2Trees[this] = defaultTreeName;
		}
		else
			m_Root->GetTreeMap().Name2Trees[{this, id}] = defaultTreeName;

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
		auto tree = pAgent->GetBehavior()->GetMappedTree(this);
		if (tree != nullptr)
		{
			if (m_Inputs.size() > 0 || m_Outputs.size() > 0)
			{
				LocalMemoryInOut inout(pAgent, m_Inputs.size() > 0 ? &m_Inputs : nullptr, m_Outputs.size() > 0 ? &m_Outputs : nullptr);
				return tree->RootExecute(pAgent, m_RunningContext != nullptr ? NS_RUNNING : NS_INVALID, &inout);
			}
			else
			{
				return tree->RootExecute(pAgent, m_RunningContext != nullptr ? NS_RUNNING : NS_INVALID);
			}
		}
		return NS_FAILURE;
	}
}
