#ifndef _YBEHAVIOR_BEHAVIORTREEMGR_H_
#define _YBEHAVIOR_BEHAVIORTREEMGR_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <unordered_map>

namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	class BehaviorNode;
	class BehaviorTree;
	class TreeInfo
	{
	public:
		TreeInfo()
			: m_OriginalTree(nullptr)
		{

		}
		~TreeInfo();
		BehaviorTree* m_OriginalTree;
	};

	class YBEHAVIOR_API TreeMgr
	{
	public:
		BehaviorTree* GetTree(const STRING& name);
		static TreeMgr* Instance();
	protected:
		BehaviorTree* _LoadOneTree(const STRING& name);
		bool _LoadOneNode(BehaviorNode* node, const pugi::xml_node& data, UINT& parentUID);
	private:
		static TreeMgr* s_Instance;
		TreeMgr(){}
		~TreeMgr();

		std::unordered_map<STRING, TreeInfo*> m_Trees;
	};
}

#endif