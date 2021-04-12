#ifndef _YBEHAVIOR_SWITCHCASE_H_
#define _YBEHAVIOR_SWITCHCASE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	class SwitchCaseNodeContext : public CompositeNodeContext
	{
		///> -2: normal: -1: default; 0~size-1: cases
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class SwitchCase : public CompositeNode<SwitchCaseNodeContext>
	{
		friend SwitchCaseNodeContext;
	public:
		STRING GetClassName() const override { return "SwitchCase"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		ISharedVariableEx* m_Switch;
		ISharedVariableEx* m_Cases;

		StdVector<BehaviorNodePtr> m_CasesChilds;
		BehaviorNodePtr m_DefaultChild = nullptr;
	};
}

#endif
