#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <iostream>
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"
#include "YBehavior/nodefactory.h"
#include <string.h>
#include "YBehavior/tools/common.h"
#include <stack>

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

		pugi::xml_parse_result result = doc.load_file((name + ".xml").c_str());
		LOG_BEGIN << "Loading: " << name << ".xml" << LOG_END;
		if (!result)
		{
			ERROR_BEGIN << "Load result: " << result.description() << ERROR_END;
			return nullptr;
		}

		auto rootData = doc.first_child();
		if (rootData == nullptr)
			return nullptr;

		BehaviorTree* tree = new BehaviorTree(name);

		UINT uid = 0;
		if (!_LoadOneNode(tree, rootData.first_child(), uid))
		{
			ERROR_BEGIN << "Load xml failed: " << name << ERROR_END;
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

	bool TreeMgr::_LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID)
	{
		if (node == nullptr)
			return false;

		node->Load(data);
		node->SetUID(++parentUID);
		for (auto it = data.begin(); it != data.end(); ++it)
		{
			if (strcmp(it->name(), "Node") == 0)
			{
				auto className = it->attribute("Class");
				if (className.empty())
				{
					ERROR_BEGIN << "Cant Find Class Name in: " << data.name() << ERROR_END;
					continue;
				}
				BehaviorNode* childNode = BehaviorNode::CreateNodeByName(className.value());
				if (childNode == nullptr)
				{
					ERROR_BEGIN << "Cant create node " << className.value() << " cause its not registered;" << ERROR_END;
					continue;
				}

				auto connectionName = it->attribute("Connection");
				node->AddChild(childNode, connectionName.value());

				_LoadOneNode(childNode, *it, parentUID);
			}
			else
			{

			}
		}
		node->LoadFinish();
		return true;
	}

	struct MergedTreeNode
	{
		std::list<BehaviorTree*> trees;
		std::vector<MergedTreeNode*> children;

		~MergedTreeNode()
		{
			if (m_bAutoDeleteMergedSharedData && m_MergedSharedData)
				delete m_MergedSharedData;
		}
		SharedDataEx* CreateMergedSharedData()
		{
			if (m_MergedSharedData && m_bAutoDeleteMergedSharedData)
				delete m_MergedSharedData;
			m_MergedSharedData = new SharedDataEx();
			m_bAutoDeleteMergedSharedData = true;
			return m_MergedSharedData;
		}
		SharedDataEx* GetSharedData() { return m_MergedSharedData; }
		void SetSharedData(SharedDataEx* data)
		{
			if (m_MergedSharedData && m_bAutoDeleteMergedSharedData)
				delete m_MergedSharedData;
			m_MergedSharedData = data;
			m_bAutoDeleteMergedSharedData = false;
		}
	private:
		SharedDataEx* m_MergedSharedData = nullptr;
		bool m_bAutoDeleteMergedSharedData = false;

	};

	struct TreeBuildingInfo 
	{
		BehaviorTree* tree = nullptr;
		bool sharedDataBuilt = false;
		int cycleID = -1;
	};

	struct CurrentBuildingInfo
	{
		std::list<BehaviorTree*> visitedTrees;
		std::unordered_set<BehaviorTree*> visitedTreesSet;
		std::unordered_map<BehaviorTree*, TreeBuildingInfo*> treeInfos;
		std::list<std::unordered_set<int>> sameCycle;
		int maxCycleID = -1;

		MergedTreeNode* mergedRoot = nullptr;

		~CurrentBuildingInfo()
		{
			for (auto it = treeInfos.begin(); it != treeInfos.end(); ++it)
			{
				delete it->second;
			}

			if (mergedRoot)
			{
				std::list<MergedTreeNode*> nodes;
				nodes.push_back(mergedRoot);
				while (nodes.size() > 0)
				{
					MergedTreeNode* node = nodes.front();
					nodes.pop_front();
					for (auto it = node->children.begin(); it != node->children.end(); ++it)
					{
						nodes.push_back(*it);
					}

					delete node;
				}
			}
		}

		TreeBuildingInfo* GetTreeInfo(BehaviorTree* tree)
		{
			auto it = treeInfos.find(tree);
			if (it == treeInfos.end())
			{
				TreeBuildingInfo* info = new TreeBuildingInfo();
				info->tree = tree;
				treeInfos[tree] = info;
				return info;
			}
			else
			{
				return it->second;
			}
		}

		void AddCycle(int cycle)
		{
			std::unordered_set<int> newSet;
			newSet.insert(cycle);
			sameCycle.push_back(std::move(newSet));
		}

		bool IsSameCycle(int cycle0, int cycle1)
		{
			if (cycle0 == cycle1)
				return true;

			std::unordered_set<int>* sameCycle0 = nullptr;
			std::unordered_set<int>* sameCycle1 = nullptr;

			for (auto it = sameCycle.begin(); it != sameCycle.end(); ++it)
			{
				if (sameCycle0 == nullptr && it->count(cycle0))
					sameCycle0 = &(*it);
				if (sameCycle1 == nullptr && it->count(cycle1))
					sameCycle1 = &(*it);

				if (sameCycle0 != nullptr && sameCycle1 != nullptr)
				{
					return sameCycle0 == sameCycle1;
				}
			}

			return false;
		}

		void MergeCycle(int cycle0, int cycle1)
		{
			std::unordered_set<int>* sameCycle0 = nullptr;
			std::unordered_set<int>* sameCycle1 = nullptr;

			for (auto it = sameCycle.begin(); it != sameCycle.end(); ++it)
			{
				if (sameCycle0 == nullptr && it->count(cycle0))
					sameCycle0 = &(*it);
				if (sameCycle1 == nullptr && it->count(cycle1))
					sameCycle1 = &(*it);

				if (sameCycle0 != nullptr && sameCycle1 != nullptr)
				{
					if (sameCycle0 == sameCycle1)
					{
						///> Already in same merged cycle;
						return;
					}

					///> merge the latter into the former, and erase the latter
					if (sameCycle1 == &(*it))
					{
						sameCycle0->insert(sameCycle1->begin(), sameCycle1->end());
						sameCycle.erase(it);
						return;
					}
					if (sameCycle0 == &(*it))
					{
						sameCycle1->insert(sameCycle0->begin(), sameCycle0->end());
						sameCycle.erase(it);
						return;
					}
				}
			}

			if (sameCycle0 == nullptr && sameCycle1 == nullptr)
			{
				///> create new merged cycle
				std::unordered_set<int> newSet;
				newSet.insert(cycle0);
				newSet.insert(cycle1);
				sameCycle.push_back(std::move(newSet));
				return;
			}
			if (sameCycle0 != nullptr)
				sameCycle0->insert(cycle1);
			else
				sameCycle1->insert(cycle0);
		}
	};
	bool _BuildSharedData(BehaviorTree* current, BehaviorTree* parent, CurrentBuildingInfo& builingInfo)
	{
		TreeBuildingInfo* treeInfo = builingInfo.GetTreeInfo(current);
		///> There's cycle
		if (builingInfo.visitedTreesSet.count(current))
		{
			std::unordered_set<int> mergedCycle;
			builingInfo.AddCycle(++builingInfo.maxCycleID);
			for (auto it = builingInfo.visitedTrees.rbegin(); it != builingInfo.visitedTrees.rend(); ++it)
			{
				TreeBuildingInfo* previousTree = builingInfo.GetTreeInfo(*it);
				
				///> Already in a cycle
				if (previousTree->cycleID >= 0 && mergedCycle.count(previousTree->cycleID) == 0)
				{
					builingInfo.MergeCycle(builingInfo.maxCycleID, previousTree->cycleID);
					mergedCycle.insert(previousTree->cycleID);
				}
				else
				{
					previousTree->cycleID = builingInfo.maxCycleID;
				}

				if (current == *it)
					break;
			}

			return false;
		}

		if (treeInfo->cycleID < 0)
		{
			builingInfo.visitedTreesSet.insert(current);
			builingInfo.visitedTrees.push_back(current);

			bool allChildrenBuilt = true;
			for (auto it = current->GetSubTrees().begin(); it != current->GetSubTrees().end(); ++it)
			{
				bool built = _BuildSharedData(*it, current, builingInfo);
				if (built)
					current->GetSharedData()->Merge(*(*it)->GetSharedData(), false);

				allChildrenBuilt &= built;
			}

			builingInfo.visitedTreesSet.erase(current);
			builingInfo.visitedTrees.pop_back();

			treeInfo->sharedDataBuilt = allChildrenBuilt;

			return allChildrenBuilt;
		}

		return false;
	}

	void _BuildMergedTree(BehaviorTree* current, TreeBuildingInfo* info, MergedTreeNode* node, CurrentBuildingInfo& builingInfo);
	void BuildMergedTree(BehaviorTree* root, CurrentBuildingInfo& builingInfo)
	{
		MergedTreeNode* node = new MergedTreeNode();
		node->trees.push_back(root);
		builingInfo.mergedRoot = node;
		TreeBuildingInfo* info = builingInfo.GetTreeInfo(root);
		_BuildMergedTree(root, info, node, builingInfo);
	}

	void _BuildMergedTree(BehaviorTree* current, TreeBuildingInfo* info, MergedTreeNode* node, CurrentBuildingInfo& builingInfo)
	{
		std::list<BehaviorTree*> trees;
		std::unordered_set<BehaviorTree*> visited;
		visited.insert(current);
		trees.push_back(current);
		while (trees.size() > 0)
		{
			BehaviorTree* tree = trees.front();
			trees.pop_front();
			bool bCreate = false;
			for (auto it = tree->GetSubTrees().begin(); it != tree->GetSubTrees().end(); ++it)
			{
				if (visited.count(*it))
					continue;
				TreeBuildingInfo* childInfo = builingInfo.GetTreeInfo(*it);
				if (childInfo->sharedDataBuilt)
					continue;
				///> every child will create a MergedTreeNode, cause parent belongs to no one of the cycles
				if (info->cycleID < 0)
				{
					bCreate = true;
				}
				else
				{
					if (childInfo->cycleID < 0)
						bCreate = true;
					else
					{
						bCreate = !builingInfo.IsSameCycle(info->cycleID, childInfo->cycleID);
					}
				}

				if (bCreate)
				{
					MergedTreeNode* childNode = new MergedTreeNode();
					childNode->trees.push_back(*it);
					node->children.push_back(childNode);
					_BuildMergedTree(*it, childInfo, childNode, builingInfo);
				}
				else
				{
					node->trees.push_back(*it);
					trees.push_back(*it);
					visited.insert(*it);
				}
			}
		}
	}

	void _FinalBuild(MergedTreeNode* node, CurrentBuildingInfo& buildingInfo)
	{
		if (node->trees.size() == 0)
		{
			LOG_BEGIN << "Something is wrong that MergedTreeNode has no tree." << LOG_END;
			return;
		}
		SharedDataEx* commonData;
		if (node->trees.size() != 1)
		{
			commonData = node->CreateMergedSharedData();
			for (auto it = node->trees.begin(); it != node->trees.end(); ++it)
			{
				commonData->Merge(*(*it)->GetSharedData(), false);
			}
		}
		else
		{
			node->SetSharedData(node->trees.front()->GetSharedData());
			commonData = node->GetSharedData();
		}
		if (node->children.size() > 0)
		{
			for (auto it = node->children.begin(); it != node->children.end(); ++it)
			{
				_FinalBuild(*it, buildingInfo);
				commonData->Merge(*(*it)->GetSharedData(), false);
			}
		}

		if (node->trees.size() != 1)
		{
			for (auto it = node->trees.begin(); it != node->trees.end(); ++it)
			{
				(*it)->GetSharedData()->Merge(*commonData, true);
			}
		}
	}
	void FinalBuild(CurrentBuildingInfo& buildingInfo)
	{
		_FinalBuild(buildingInfo.mergedRoot, buildingInfo);
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
		info->SetLatestTree(tree);
		info->ChangeReferenceCount(true, bToAgent);

		bool bNoChildren = true;
		if (m_ToBeLoadedTree.size() > 0)
		{
			std::list<STRING> tobeload;
			tobeload.swap(m_ToBeLoadedTree);
			while (tobeload.size() > 0)
			{
				BehaviorTree* subTree;
				
				_GetTree(tobeload.front(), subTree, false);

				tobeload.pop_front();
				tree->AddSubTree(subTree);
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

	void TreeInfo::Print()
	{
		for (auto it = m_TreeVersions.begin(); it != m_TreeVersions.end(); ++it)
		{
			std::cout << "version " << it->first << ", agentcount " << it->second->agentReferenceCount << ", treecount " << it->second->treeReferenceCount << std::endl;
		}
	}

	TreeInfo::TreeInfo()
		: m_LatestVersion(nullptr)
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

		{
			if (version->tree->GetSubTrees().size() > 0)
			{
				for (auto it = version->tree->GetSubTrees().begin(); it != version->tree->GetSubTrees().end(); ++it)
				{
					TreeMgr::Instance()->ReturnTree(*it, false);
				}
			}
		}

		m_TreeVersions.erase(version->version);

		delete version->tree;
		delete version;
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
		m_LatestVersion = pVersion;
		m_TreeVersions[pVersion->version] = pVersion;

		return pVersion;
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
		tree->SetVersion(m_LatestVersion->version);
	}

	void TreeInfo::ChangeReferenceCount(bool bInc, bool bAgent, int versionNum /*= -1*/)
	{
		TreeVersion* version = nullptr;
		if (versionNum < 0)
			version = m_LatestVersion;
		else
		{
			auto it = m_TreeVersions.find(versionNum);
			if (it != m_TreeVersions.end())
				version = it->second;
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
