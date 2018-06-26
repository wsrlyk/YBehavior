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
		NodeState Update(AgentPtr pAgent) override;
		void OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		BehaviorNodePtr m_InitChild = nullptr;
		BehaviorNodePtr m_CondChild = nullptr;
		BehaviorNodePtr m_IncChild = nullptr;
		BehaviorNodePtr m_MainChild = nullptr;
	};

	class ForEach : public SingleChildNode
	{
	public:
		STRING GetClassName() const override { return "ForEach"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		void OnLoaded(const pugi::xml_node& data) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		ISharedVariableEx* m_Collection = nullptr;
		ISharedVariableEx* m_Current = nullptr;
	};
}

#endif
