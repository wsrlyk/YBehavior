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
		TreeVersion* CreateVersion();
		BehaviorTree* GetLatestTree() { return m_LatestVersion ? m_LatestVersion->tree : nullptr; }
		void IncreaseLatestVesion();

		void SetLatestTree(BehaviorTree* tree);
		void ChangeReferenceCount(bool bInc, bool bAgent, int versionNum = -1);
		void Print();
	private:
		TreeVersion* m_LatestVersion;
		std::unordered_map<int, TreeVersion*> m_TreeVersions;
	};

	class YBEHAVIOR_API TreeMgr
	{
	public:
		BehaviorTree * GetTree(const STRING& name);
		///> Mark this tree dirty to reload it when GetTree
		void ReloadTree(const STRING& name);
		void ReloadAll();
		void ReturnTree(BehaviorTree* tree, bool bFromAgent);
		static TreeMgr* Instance();
		void Print();

		void PushToBeLoadedTree(const STRING& name) { m_ToBeLoadedTree.push_back(name); }
	protected:
		bool _GetTree(const STRING& name, BehaviorTree * &tree, bool bToAgent);
		void _CheckSubTree(const STRING& name, BehaviorTree* current, std::unordered_set<BehaviorTree*>& visited, std::list<BehaviorTree*>& visitedStack);
		BehaviorTree * _LoadTree(const STRING& name);
		BehaviorTree* _LoadOneTree(const STRING& name);
		bool _LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID);
	private:
		static TreeMgr* s_Instance;
		TreeMgr() {}
		~TreeMgr();

		std::unordered_map<STRING, TreeInfo*> m_Trees;

		std::list<STRING> m_ToBeLoadedTree;
	};
}

#endif