#ifndef _YBEHAVIOR_SWITCHCASE_H_
#define _YBEHAVIOR_SWITCHCASE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	class YBEHAVIOR_API SwitchCaseContext : public RunningContext
	{
	public:
		///> -2: normal: -1: default; 0~size-1: cases
		int Current = -2;
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
		virtual NodeState Update(AgentPtr pAgent);
		virtual bool OnLoaded(const pugi::xml_node& data);
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		ISharedVariableEx* m_Switch;
		ISharedVariableEx* m_Cases;

		std::vector<BehaviorNodePtr> m_CasesChilds;
		BehaviorNodePtr m_DefaultChild = nullptr;
		ContextContainer<SwitchCaseContext> m_RCContainer;
	};
}

#endif
