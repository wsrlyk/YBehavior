#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodefactory.h"
#include <string.h>
#include "YBehavior/tools/common.h"
#include "YBehavior/tools/treemgrhelper.hpp"

namespace YBehavior
{

	TreeMgr* TreeMgr::Instance()
	{
		if (!s_Instance)
		{
			s_Instance = new TreeMgr();
		}
		return s_Instance;
	}

	BehaviorTree * TreeMgr::_LoadTree(const STRING& name)
	{
		return _LoadOneTree(name);
	}

	BehaviorTree* TreeMgr::_LoadOneTree(const STRING& name)
	{
		pugi::xml_document doc;

		pugi::xml_parse_result result = doc.load_file((m_WorkingDir + name + ".xml").c_str());
		LOG_BEGIN << "Loading: " << name << ".xml" << LOG_END;
		if (result.status)
		{
			ERROR_BEGIN << "Load result: " << result.description() << ERROR_END;
			return nullptr;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;

		BehaviorTree* tree = new BehaviorTree(name);

		UINT uid = 0;
		if (!_LoadOneNode(tree, rootData.first_child(), uid, tree))
		{
			ERROR_BEGIN << "Load xml failed: " << name << ERROR_END;
			delete tree;
			return nullptr;
		}

#ifdef DEBUGGER
		xml_string_writer writer;
		rootData.print(writer, PUGIXML_TEXT("\t"), pugi::format_indent | pugi::format_raw);
		writer.result.erase(std::remove_if(writer.result.begin(), writer.result.end(), ::isspace), writer.result.end());
		tree->SetHash(Utility::Hash(writer.result));
#endif
		return tree;
	}

	bool TreeMgr::_LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID, BehaviorTree* root)
	{
		if (node == nullptr)
			return false;

		node->SetRoot(root);
		if (!node->Load(data))
			return false;
		node->SetUID(++parentUID);
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			if (strcmp(it->name(), "Node") == 0)
			{
				auto className = it->attribute("Class");
				if (className.empty())
				{
					ERROR_BEGIN << "Cant Find Class Name in: " << data.name() << ERROR_END;
					return false;
				}
				BehaviorNode* childNode = BehaviorNode::CreateNodeByName(className.value());
				if (childNode == nullptr)
				{
					ERROR_BEGIN << "Cant create node " << className.value() << " cause its not registered;" << ERROR_END;
					return false;
				}

				auto connectionName = it->attribute("Connection");
				if (!node->AddChild(childNode, connectionName.value()))
				{
					delete childNode;
					return false;
				}

				if (!_LoadOneNode(childNode, *it, parentUID, root))
					return false;
			}
			else
			{

			}
		}
		node->LoadFinish();
		return true;
	}

	BehaviorTree * TreeMgr::GetTree(const STRING& name)
	{
		BehaviorTree *tree;
		if (_GetTree(name, tree, true))
			return tree;

		CurrentBuildingInfo buildingInfo;
		if (!_BuildSharedData(tree, nullptr, buildingInfo))
		{
			//_ProcessCycle(buildingInfo);
			//_FinalBuild(tree, buildingInfo);
			BuildMergedTree(tree, buildingInfo);
			FinalBuild(buildingInfo);
		}

		return tree;
	}

	bool TreeMgr::_GetTree(const STRING& name, BehaviorTree* &tree, bool bToAgent)
	{
		TreeInfo* info;
		auto it = m_Trees.find(name);
		if (it != m_Trees.end())
		{
			tree = it->second->GetLatestTree();
			if (tree)
			{
				it->second->ChangeReferenceCount(true, bToAgent);
				return true;
			}
			else
				info = it->second;
		}
		else
		{
			info = new TreeInfo();
			m_Trees[name] = info;
		}

		
		tree = _LoadTree(name);
		if (!tree)
			return true;
		info->SetLatestTree(tree);
		info->ChangeReferenceCount(true, bToAgent);

		bool bNoChildren = true;
		if (m_ToBeLoadedTree.size() > 0)
		{
			bool bLoadChidrenFailed = false;
			std::unordered_set<STRING> tobeload;
			tobeload.swap(m_ToBeLoadedTree);
			for (auto it2 = tobeload.begin(); it2 != tobeload.end(); ++it2)
			{
				BehaviorTree* subTree;
				
				_GetTree(*it2, subTree, false);
				if (subTree)
					tree->AddSubTree(subTree);
				else
				{
					bLoadChidrenFailed = true;
					break;
				}
			}
			if (bLoadChidrenFailed)
			{
				info->ChangeReferenceCount(false, bToAgent);
				info->RevertVersion();
				tree = nullptr;
				return true;
			}
			bNoChildren = false;
		}

		return bNoChildren;
	}

	void TreeMgr::_CheckSubTree(const STRING& name, BehaviorTree* current, std::unordered_set<BehaviorTree*>& visited, std::list<BehaviorTree*>& visitedStack)
	{
		if (visited.count(current))
			return;
		visitedStack.push_back(current);
		visited.insert(current);
		for (auto it = current->GetSubTrees().begin(); it != current->GetSubTrees().end(); ++it)
		{
			if ((*it)->GetTreeNameWithPath() == name)
			{
				///> increase all the ancestors's version
				for (auto it2 = visitedStack.rbegin(); it2 != visitedStack.rend(); ++it2)
				{
					auto it3 = m_Trees.find((*it2)->GetTreeNameWithPath());
					if (it3 != m_Trees.end())
					{
						it3->second->IncreaseLatestVesion();
					}
				}
			}
			else
			{
				_CheckSubTree(name, *it, visited, visitedStack);
			}
		}

		visitedStack.pop_back();
	}

	void TreeMgr::ReloadTree(const STRING& name)
	{
		auto it = m_Trees.find(name);
		if (it != m_Trees.end())
		{
			it->second->IncreaseLatestVesion();
		}

		///> find all trees reference to this tree, and inc their version

		std::unordered_set<BehaviorTree*> visited;
		std::list<BehaviorTree*> visitedStack;
		for (auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
		{
			BehaviorTree* tree = it->second->GetLatestTree();
			if (tree == nullptr)
			{
				///> Already reloaded.
				continue;
			}

			if (tree->GetSubTrees().size() == 0)
				continue;

			visited.clear();
			visitedStack.push_back(tree);

			_CheckSubTree(name, tree, visited, visitedStack);
		}
	}

	void TreeMgr::ReloadAll()
	{
		for (auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
		{
			it->second->IncreaseLatestVesion();
		}
	}

	void TreeMgr::ReturnTree(BehaviorTree* tree, bool bFromAgent)
	{
		if (tree == nullptr)
			return;

		auto it = m_Trees.find(tree->GetTreeNameWithPath());
		if (it != m_Trees.end())
		{
			it->second->ChangeReferenceCount(false, bFromAgent, tree->GetVersion());
		}
	}

	TreeMgr::~TreeMgr()
	{
		for(auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
			delete it->second;
		m_Trees.clear();
	}

	TreeMgr* TreeMgr::s_Instance;

	void TreeMgr::Print()
	{
		std::cout << "Print all trees" << std::endl;
		for (auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
		{
			std::cout << it->first << std::endl;
			it->second->Print();
		}
		std::cout << "Print all trees end." << std::endl;
	}

	void TreeMgr::SetWorkingDir(const STRING& dir)
	{
		m_WorkingDir = dir;

		if (m_WorkingDir == "")
			return;

		size_t len = m_WorkingDir.length();
		if (m_WorkingDir[len - 1] != '\\' && m_WorkingDir[len - 1] != '/')
		{
			m_WorkingDir.append(1, '/');
		}
	}

	void TreeMgr::GarbageCollection()
	{
		std::unordered_set<BehaviorTree*> hasAgent;
		std::unordered_set<BehaviorTree*> noAgent;

		std::list<BehaviorTree*> temp;

		for (auto it = m_Trees.begin(); it != m_Trees.end(); ++it)
		{
			TreeInfo* info = it->second;
			for (auto it2 = info->GetVersions().begin(); it2 != info->GetVersions().end(); ++it2)
			{
				TreeVersion* version = it2->second;
				if (version == info->GetLatestVersion())
					continue;

				if (version->agentReferenceCount > 0)
				{
					hasAgent.insert(version->tree);
					///> Get all children and mark them as having reference.
					temp.clear();
					temp.push_back(version->tree);
					while (!temp.empty())
					{
						BehaviorTree* tree = temp.front();
						temp.pop_front();
						if (hasAgent.count(tree))
							continue;
						hasAgent.insert(tree);
						noAgent.erase(tree);

						for (auto it3 = tree->GetSubTrees().begin(); it3 != tree->GetSubTrees().end(); ++it3)
						{
							temp.push_back(*it3);
						}
					}
				}
				else
				{
					noAgent.insert(version->tree);
				}
			}
		}

		for (auto it = noAgent.begin(); it != noAgent.end(); ++it)
		{
			(*it)->ClearSubTree();

			auto it2 = m_Trees.find((*it)->GetTreeNameWithPath());
			if (it2 == m_Trees.end())
			{
				ERROR_BEGIN << "Cant find info about tree: " << (*it)->GetTreeNameWithPath() << ERROR_END;
				continue;
			}

			it2->second->RemoveVersion((*it)->GetVersion());
		}
	}

	///>//////////////////////////////////////////////////////////////////////////////////////////
	///>//////////////////////////////////////////////////////////////////////////////////////////
	///>//////////////////////////////////////////////////////////////////////////////////////////

	void TreeInfo::Print()
	{
		for (auto it = m_TreeVersions.begin(); it != m_TreeVersions.end(); ++it)
		{
			std::cout << "version " << it->first << ", agentcount " << it->second->agentReferenceCount << ", treecount " << it->second->treeReferenceCount << std::endl;
		}
	}

	TreeInfo::TreeInfo()
		: m_LatestVersion(nullptr)
		, m_PreviousVersion(nullptr)
	{
		CreateVersion();
	}

	TreeInfo::~TreeInfo()
	{
		for (auto it = m_TreeVersions.begin(); it != m_TreeVersions.end(); ++it)
		{
			delete it->second->tree;
			delete it->second;
		}
		m_TreeVersions.clear();
	}

	void TreeInfo::TryRemoveVersion(TreeVersion* version)
	{
		if (version == nullptr || version->tree == nullptr || version->GetReferenceCount() > 0)
			return;
		RemoveVersion(version);
	}

	void TreeInfo::RemoveVersion(TreeVersion* version)
	{
		if (version)
		{
			if (version->tree->GetSubTrees().size() > 0)
			{
				for (auto it = version->tree->GetSubTrees().begin(); it != version->tree->GetSubTrees().end(); ++it)
				{
					TreeMgr::Instance()->ReturnTree(*it, false);
				}
			}

			m_TreeVersions.erase(version->version);

			if (version->tree)
				delete version->tree;

			if (m_PreviousVersion == version)
				m_PreviousVersion = nullptr;
			if (m_LatestVersion == version)
				m_LatestVersion = nullptr;
			delete version;
		}
	}

	TreeVersion* TreeInfo::CreateVersion()
	{
		TreeVersion* pVersion = new TreeVersion();
		if (m_LatestVersion == nullptr)
		{
			pVersion->version = 0;
		}
		else
		{
			pVersion->version = m_LatestVersion->version + 1;

			///> Check if the current latest version has no reference. Remove it if true
			{
				TryRemoveVersion(m_LatestVersion);
			}
		}
		m_PreviousVersion = m_LatestVersion;
		m_LatestVersion = pVersion;
		m_TreeVersions[pVersion->version] = pVersion;

		return pVersion;
	}

	void TreeInfo::RevertVersion()
	{
		TryRemoveVersion(m_LatestVersion);
		m_LatestVersion = m_PreviousVersion;
		m_PreviousVersion = nullptr;
	}

	void TreeInfo::IncreaseLatestVesion()
	{
		if (m_LatestVersion == nullptr || m_LatestVersion->tree == nullptr)
			return;

		CreateVersion();
	}

	void TreeInfo::SetLatestTree(BehaviorTree* tree)
	{
		if (m_LatestVersion == nullptr)
			CreateVersion();
		m_LatestVersion->tree = tree;
		tree->SetVersion(m_LatestVersion);
	}

	void TreeInfo::ChangeReferenceCount(bool bInc, bool bAgent, TreeVersion* version)
	{
		if (version == nullptr)
			version = m_LatestVersion;
		else
		{
			auto it = m_TreeVersions.find(version->version);
			if (it != m_TreeVersions.end() && it->second != version)
				version = nullptr;
		}

		if (version == nullptr)
			return;

		if (bInc)
		{
			if (bAgent)
				++(version->agentReferenceCount);
			else
				++(version->treeReferenceCount);
		}
		else
		{
			if (bAgent)
				--(version->agentReferenceCount);
			else
				--(version->treeReferenceCount);

			if (m_LatestVersion != version)
			{
				///> Old version has no reference, remove it
				TryRemoveVersion(version);
			}
		}
	}

}
