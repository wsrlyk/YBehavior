#include "YBehavior/nodes/subtree.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/mgrs.h"
#include "YBehavior/fsm/behavior.h"
#include "YBehavior/variablecreation.h"
#include <cstring>
#include "YBehavior/fsm/context.h"

namespace YBehavior
{
	bool SubTree::OnLoaded(const pugi::xml_node& data)
	{
		if (!m_Root)
		{
			ERROR_BEGIN_NODE_HEAD << "No Root" << ERROR_END;
			return false;
		}

		VariableCreation::CreateVariable(this, m_Identification, "Identification", data, false);
		if (!m_Identification)
		{
			return false;
		}
		
		VariableCreation::CreateVariable(this, m_TreeName, "Tree", data, false);
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
			ERROR_BEGIN_NODE_HEAD << "Null Value for Tree in " << this->GetClassName() << ERROR_END;
			return false;
		}

		if (m_Root->GetTreeNameWithPath() != m_FinalTreeName)
			Mgrs::Instance()->GetTreeMgr()->PushToBeLoadedTree(m_FinalTreeName);*/
		//if (defaultTreeName.empty())
		//{
		//	//ERROR_BEGIN_NODE_HEAD << "Null Value for Tree in " << this->GetClassName() << ERROR_END;
		//	//return false;
		//	return true;
		//}

		if (id.empty())
		{
			if (!defaultTreeName.empty())
				m_Root->GetTreeMap().Node2Trees[this] = defaultTreeName;
		}
		else
		{
			m_Root->GetTreeMap().Name2Trees[std::make_tuple(this, id)] = defaultTreeName;
		}

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

			VariableCreation::CreateVariable(this, pVariable, it->name(), data, ST_NONE);
			if (!pVariable)
			{
				ERROR_BEGIN_NODE_HEAD << "Failed to Create " << data.name() << ERROR_END;
				return false;
			}
			//if (container.count(it->name()) > 0)
			//{
			//	ERROR_BEGIN_NODE_HEAD << "Duplicate " << data.name() << " Variable: " << it->name() << ERROR_END;
			//	return false;
			//}
			container.push_back(pVariable);
		}

		return true;
	}

	void SubTreeNodeContext::_OnInit()
	{
		TreeNodeContext::_OnInit();
		m_Stage = 0;
		m_pInOut = nullptr;
	}

	void SubTreeNodeContext::_OnDestroy()
	{
		if (m_pInOut)
		{
			ObjectPoolStatic<LocalMemoryInOut>::Recycle(m_pInOut);
			m_pInOut = nullptr;
		}
	}

	NodeState SubTreeNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		if (m_Stage == 0)
		{
			++m_Stage;
			auto tree = pAgent->GetBehavior()->GetMappedTree(m_pNode);
			if (tree != nullptr)
			{
				SubTree* pNode = (SubTree*)m_pNode;
				if (pNode->m_Inputs.size() > 0 || pNode->m_Outputs.size() > 0)
				{
					m_pInOut = ObjectPoolStatic<LocalMemoryInOut>::Get();
					m_pInOut->Set(pAgent, pNode->m_Inputs.size() > 0 ? &pNode->m_Inputs : nullptr, pNode->m_Outputs.size() > 0 ? &pNode->m_Outputs : nullptr);
					pAgent->GetTreeContext()->PushCallStack(tree->CreateRootContext(m_pInOut));
					return NS_RUNNING;
				}
				else
				{
					pAgent->GetTreeContext()->PushCallStack(tree->CreateRootContext(nullptr));
					return NS_RUNNING;
				}
			}
			else
				return NS_FAILURE;
		}
		else
			return lastState;
	}

}
