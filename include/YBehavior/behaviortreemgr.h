#ifndef _YBEHAVIOR_BEHAVIORTREEMGR_H_
#define _YBEHAVIOR_BEHAVIORTREEMGR_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddata.h"

namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	class BehaviorNode;

	class YBEHAVIOR_API TreeMgr
	{
	public:
		void LoadOneTree(const STRING& name);
		static TreeMgr* Instance();
	protected:
		bool _LoadOneNode(BehaviorNode* node, const pugi::xml_node& data);
	private:
		static TreeMgr* s_Instance;
		TreeMgr(){}

	};
}

#endif