#ifndef _YBEHAVIOR_BEHAVIORTREEMGR_H_
#define _YBEHAVIOR_BEHAVIORTREEMGR_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <unordered_map>
#include <list>
#include <unordered_set>
#include "version.h"
#include "behaviortree.h"
#include "YBehavior/mgrs.h"

namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	typedef VersionMgr<BehaviorTree, STRING> TreeVersionMgrType;
	typedef typename TreeVersionMgrType::InfoType TreeInfoType;
	typedef typename TreeVersionMgrType::InfoType::VersionType TreeVersionType;

	class TreeMgr
	{
	public:
		TreeMgr() {}
		~TreeMgr();
		BehaviorTree * GetTree(const STRING& name);
		///> Mark this tree dirty to reload it when GetTree
		void ReloadTree(const STRING& name);
		void ReloadAll();
		void ReturnTree(BehaviorTree* tree);
		//static TreeMgr* Instance();
		void Print();
		void SetWorkingDir(const STRING& dir);
		void SetLoadDataCallback(LoadDataDelegate callback) { m_LoadDataCallback = callback; }
	protected:
		bool _GetTree(const STRING& name, BehaviorTree * &tree, bool bToAgent);
		BehaviorTree * _LoadTree(const STRING& name);
		bool _LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID, BehaviorTree* root);
	private:
		//static TreeMgr* s_Instance;

		VersionMgr<BehaviorTree, STRING> m_VersionMgr;
		STRING m_WorkingDir;

		LoadDataDelegate m_LoadDataCallback = nullptr;
	};
}

#endif