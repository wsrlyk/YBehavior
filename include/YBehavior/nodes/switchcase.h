#ifndef _YBEHAVIOR_SWITCHCASE_H_
#define _YBEHAVIOR_SWITCHCASE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class SwitchCase : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "SwitchCase"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual bool OnLoaded(const pugi::xml_node& data);
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		ISharedVariableEx* m_Switch;
		ISharedVariableEx* m_Cases;

		std::vector<BehaviorNodePtr> m_CasesChilds;
		BehaviorNodePtr m_DefaultChild = nullptr;
	};
}

#endif
