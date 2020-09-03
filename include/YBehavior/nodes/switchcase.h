#ifndef _YBEHAVIOR_SWITCHCASE_H_
#define _YBEHAVIOR_SWITCHCASE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	class SwitchCaseContext : public RunningContext
	{
	public:
		///> -2: normal: -1: default; 0~size-1: cases
		int Current = -2;
	protected:
		void _OnReset() override
		{
			Current = -2;
		}
	};

	class SwitchCase : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "SwitchCase"; }
		SwitchCase()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		ISharedVariableEx* m_Switch;
		ISharedVariableEx* m_Cases;

		StdVector<BehaviorNodePtr> m_CasesChilds;
		BehaviorNodePtr m_DefaultChild = nullptr;
		ContextContainer<SwitchCaseContext> m_RCContainer;
	};
}

#endif
