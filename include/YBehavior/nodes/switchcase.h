#ifndef _YBEHAVIOR_SWITCHCASE_H_
#define _YBEHAVIOR_SWITCHCASE_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class SwitchCaseNodeContext : public CompositeNodeContext
	{
		///> -2: normal: -1: default; 0~size-1: cases
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class IVariableCompareHelper;
	class SwitchCase : public CompositeNode<SwitchCaseNodeContext>
	{
		friend SwitchCaseNodeContext;
	public:
		TREENODE_DEFINE(SwitchCase)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(TreeNode * child, const STRING & connection) override;

		ISharedVariableEx* m_Switch;
		ISharedVariableEx* m_Cases;

		StdVector<TreeNodePtr> m_CasesChilds;
		TreeNodePtr m_DefaultChild = nullptr;

		const IVariableCompareHelper* m_pHelper;
	};
}

#endif
