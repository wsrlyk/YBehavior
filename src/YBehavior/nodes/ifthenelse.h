#ifndef _YBEHAVIOR_IFTHENELSE_H_
#define _YBEHAVIOR_IFTHENELSE_H_

#include "YBehavior/treenode.h"

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
		void OnAddChild(TreeNode* child, const STRING& connection) override;

		TreeNode* m_If{};
		TreeNode* m_Then{};
		TreeNode* m_Else{};
	};
}

#endif
