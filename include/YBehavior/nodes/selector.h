#ifndef _YBEHAVIOR_SELECTOR_H_
#define _YBEHAVIOR_SELECTOR_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	class Selector : public CompositeNode<>
	{
	public:
		STRING GetClassName() const override { return "Selector"; }
		Selector()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		IndexIterator m_Iterator;
		ContextContainer<VectorTraversalContext> m_RCContainer;
	};

	class RandomSelector : public CompositeNode<>
	{
	public:
		STRING GetClassName() const override { return "RandomSelector"; }
		RandomSelector()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;
		IndexIterator m_Iterator;
		RandomIndex m_RandomIndex;
		ContextContainer<RandomVectorTraversalContext> m_RCContainer;
	};
}

#endif
