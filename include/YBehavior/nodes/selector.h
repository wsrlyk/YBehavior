#ifndef _YBEHAVIOR_SELECTOR_H_
#define _YBEHAVIOR_SELECTOR_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class Selector : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "Selector"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
	};

	class RandomSelector : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "RandomSelector"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;
		RandomIndex m_RandomIndex;
	};
}

#endif
