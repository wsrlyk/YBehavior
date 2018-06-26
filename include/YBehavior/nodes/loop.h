#ifndef _YBEHAVIOR_LOOP_H_
#define _YBEHAVIOR_LOOP_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/sharedvariableex.h"

namespace YBehavior
{
	class For : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "For"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual void OnLoaded(const pugi::xml_node& data);
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		BehaviorNodePtr m_InitChild = nullptr;
		BehaviorNodePtr m_CondChild = nullptr;
		BehaviorNodePtr m_IncChild = nullptr;
		BehaviorNodePtr m_MainChild = nullptr;
	};
}

#endif
