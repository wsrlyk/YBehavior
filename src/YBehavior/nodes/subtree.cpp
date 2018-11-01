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
		TYPEID typeID = CreateVariable(m_TreeName, "Tree", data, ST_SINGLE, Utility::CONST_CHAR);
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
		if (strcmp(data.name(), "In") == 0)
		{
			return _TryCreateFromTo(data, m_Inputs);
		}
		else if (strcmp(data.name(), "Out") == 0)
		{
			return _TryCreateFromTo(data, m_Outputs);
		}
		return true;
	}

	bool SubTree::_TryCreateFromTo(const pugi::xml_node& data, std::vector<FromToType>& container)
	{
		ISharedVariableEx* pFrom = nullptr;
		ISharedVariableEx* pTo = nullptr;

		TYPEID typeIDFrom = CreateVariable(pFrom, "From", data, ST_NONE);
		TYPEID typeIDTo = CreateVariable(pTo, "To", data, ST_NONE);

		if (typeIDFrom != typeIDTo || typeIDFrom == Utility::INVALID_TYPE)
		{
			ERROR_BEGIN << "From & To not match in Subtree" << ERROR_END;
			return false;
		}

		container.push_back(FromToType(pFrom, pTo));
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
			LocalMemoryCopier copier(pAgent, this);
			return m_Tree->RootExecute(pAgent, m_RunningContext != nullptr ? NS_RUNNING : NS_INVALID, &copier);
		}
		return NS_FAILURE;
	}

	LocalMemoryCopier::LocalMemoryCopier(AgentPtr pAgent, SubTree* pSubTree)
		: m_pAgent(pAgent)
		, m_pSubTree(pSubTree)
		, m_TempMemory(pAgent->GetMemory()->GetMainData(), pAgent->GetMemory()->GetStackTop())
	{

	}

	void LocalMemoryCopier::OnPreExecute()
	{
		for (auto it = m_pSubTree->m_Inputs.begin(); it != m_pSubTree->m_Inputs.end(); ++it)
		{
			it->second->SetValue(m_pAgent->GetMemory(), it->first->GetValue(&m_TempMemory));
		}
	}

	void LocalMemoryCopier::OnPostExecute()
	{
		for (auto it = m_pSubTree->m_Inputs.begin(); it != m_pSubTree->m_Inputs.end(); ++it)
		{
			it->second->SetValue(&m_TempMemory, it->first->GetValue(m_pAgent->GetMemory()));
		}
	}

}
