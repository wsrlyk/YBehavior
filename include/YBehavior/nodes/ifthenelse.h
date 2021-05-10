#ifndef _YBEHAVIOR_IFTHENELSE_H_
#define _YBEHAVIOR_IFTHENELSE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	enum struct IfThenElsePhase
	{
		None,
		If,
		ThenElse,
	};

	class IfThenElseNodeContext : public CompositeNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class IfThenElse : public CompositeNode<IfThenElseNodeContext>
	{
		friend IfThenElseNodeContext;
	public:
		TREENODE_DEFINE(IfThenElse)
	protected:
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;

		BehaviorNode* m_If{};
		BehaviorNode* m_Then{};
		BehaviorNode* m_Else{};
	};
}

#endif
