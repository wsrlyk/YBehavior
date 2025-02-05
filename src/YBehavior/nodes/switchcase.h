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

	class IDataCompareHelper;
	class SwitchCase : public CompositeNode<SwitchCaseNodeContext>
	{
		friend SwitchCaseNodeContext;
	public:
		TREENODE_DEFINE(SwitchCase)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(TreeNode * child, const STRING & connection) override;

		IPin* m_Switch;
		IPin* m_Cases;

		StdVector<TreeNodePtr> m_CasesChilds;
		TreeNodePtr m_DefaultChild = nullptr;

		const IDataCompareHelper* m_pHelper;
	};
}

#endif
