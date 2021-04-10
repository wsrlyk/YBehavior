#ifndef _YBEHAVIOR_FSMRELATED_H_
#define _YBEHAVIOR_FSMRELATED_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class FSMSetCondition : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "FSMSetCondition"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		ISharedVariableEx* m_Conditions;
		bool m_IsOn;
	};

	class FSMClearConditions : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "FSMClearConditions"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
}

#endif
