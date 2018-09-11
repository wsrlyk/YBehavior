#include "YBehavior/types.h"
#include <list>
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	struct MergedTreeNode
	{
		std::list<BehaviorTree*> trees;
		StdVector<MergedTreeNode*> children;

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
		SharedDataEx * m_MergedSharedData = nullptr;
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
}