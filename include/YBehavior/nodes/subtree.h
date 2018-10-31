#ifndef _YBEHAVIOR_SUBTREE_H_
#define _YBEHAVIOR_SUBTREE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class SubTree : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "SubTree"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual bool OnLoaded(const pugi::xml_node& data);
		bool OnLoadChild(const pugi::xml_node& data) override;
	private:
		SharedVariableEx<STRING>* m_TreeName;

		BehaviorTree* m_Tree = nullptr;
	};
}

#endif
