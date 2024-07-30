#ifndef _YBEHAVIOR_BEHAVIORTREEMGR_H_
#define _YBEHAVIOR_BEHAVIORTREEMGR_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"
#include "YBehavior/version.h"
#include "YBehavior/mgrs.h"
#include "YBehavior/behaviortree.h"

namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	struct TreeMap;
	class TreeNode;
	typedef VersionMgr<BehaviorTree, STRING> TreeVersionMgrType;
	typedef typename TreeVersionMgrType::InfoType TreeInfoType;
	typedef typename TreeVersionMgrType::InfoType::VersionType TreeVersionType;

	class TreeMgr
	{
	public:
		TreeMgr() {}
		~TreeMgr();
		///> AutoLoad and Get a tree for an agent. This will inc the ref count.
		BehaviorTree * GetTree(const STRING& name);
		///> Just load a tree. Not inc the ref count.
		bool LoadTree(const STRING& name, const TreeMap*& pOutputTreeMap);
		///> Mark this tree dirty to reload it when GetTree
		void ReloadTree(const STRING& name);
		void ReloadAll();
		void ReturnTree(BehaviorTree* tree);
		//static TreeMgr* Instance();
		void Print();
		void SetWorkingDir(const STRING& dir);
	
		void Clear();
	protected:
		bool _GetTree(const STRING& name, BehaviorTree * &tree, bool bToAgent);
		BehaviorTree * _LoadTree(const STRING& name);
		bool _LoadOneNode(TreeNode* node, const pugi::xml_node& data, std::vector<TreeNode*>& treeNodesCache, BehaviorTree* root);
	private:
		//static TreeMgr* s_Instance;

		VersionMgr<BehaviorTree, STRING> m_VersionMgr;
		STRING m_WorkingDir;
	};
}

#endif