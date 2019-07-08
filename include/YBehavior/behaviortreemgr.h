#ifndef _YBEHAVIOR_BEHAVIORTREEMGR_H_
#define _YBEHAVIOR_BEHAVIORTREEMGR_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <unordered_map>
#include <list>
#include <unordered_set>

namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	class BehaviorNode;
	class BehaviorTree;
	class BehaviorID;
	struct TreeVersion
	{
		int version = -1;
		BehaviorTree* tree = nullptr;
		int agentReferenceCount = 0;
		int treeReferenceCount = 0;
		int GetReferenceCount() { return agentReferenceCount + treeReferenceCount; }
	};
	class TreeInfo
	{
	public:
		TreeInfo();
		~TreeInfo();

		void TryRemoveVersion(TreeVersion* version);
		void RemoveVersion(TreeVersion* version);
		TreeVersion* CreateVersion();
		void RevertVersion();
		BehaviorTree* GetLatestTree() { return m_LatestVersion ? m_LatestVersion->tree : nullptr; }
		inline TreeVersion* GetLatestVersion() { return m_LatestVersion; }
		void IncreaseLatestVesion();

		void SetLatestTree(BehaviorTree* tree);
		void ChangeReferenceCount(bool bInc, bool bAgent, TreeVersion* version = nullptr);
		void Print();

		inline std::unordered_map<int, TreeVersion*>& GetVersions() { return m_TreeVersions; }
	private:
		TreeVersion * m_LatestVersion;
		TreeVersion* m_PreviousVersion;
		std::unordered_map<int, TreeVersion*> m_TreeVersions;
	};

	class YBEHAVIOR_API TreeMgr
	{
	public:
		TreeMgr() {}
		~TreeMgr();
		BehaviorTree * GetTree(const STRING& name, const std::vector<STRING>* subs);
		///> Mark this tree dirty to reload it when GetTree
		void ReloadTree(const STRING& name);
		void ReloadAll();
		void ReturnTree(BehaviorTree* tree, bool bFromAgent);
		//static TreeMgr* Instance();
		void Print();
		void SetWorkingDir(const STRING& dir);
		void PushToBeLoadedTree(const STRING& name) { m_ToBeLoadedTree.insert(name); }
		inline const std::vector<STRING>* GetToBeReplacedSubs() const { return m_ToBeReplacedSubs; }
		void GarbageCollection();
	protected:
		bool _GetTree(const STRING& name, BehaviorTree * &tree, bool bToAgent);
		void _CheckSubTree(const STRING& name, BehaviorTree* current, std::unordered_set<BehaviorTree*>& visited, std::list<BehaviorTree*>& visitedStack);
		BehaviorTree * _LoadTree(BehaviorID* id);
		bool _LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID, BehaviorTree* root);
	private:
		//static TreeMgr* s_Instance;

		std::unordered_map<BehaviorID*, TreeInfo*> m_Trees;
		std::unordered_map<STRING, std::vector<BehaviorID*>> m_TreeIDs;

		std::unordered_set<STRING> m_ToBeLoadedTree;
		///> When a tree (including its subtrees) is loading, the id is not and cant be built yet.
		///> So when one or more of its subtrees is just itself, the id cant be found in the map, thus
		///> another new id would be created, which shouldnot happen. If a buiding id is in this set,
		///> avoid use IsSame function to determine if two trees are the same.
		std::unordered_set<BehaviorID*> m_ToBeBuildID;
		const std::vector<STRING>* m_ToBeReplacedSubs;

		STRING m_WorkingDir;
	};
}

#endif