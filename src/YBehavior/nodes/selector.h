#ifndef _YBEHAVIOR_SELECTOR_H_
#define _YBEHAVIOR_SELECTOR_H_

#include "YBehavior/treenode.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class SelectorNodeContext : public CompositeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		IndexIterator m_Iterator;
	};
	class Selector : public CompositeNode<SelectorNodeContext>
	{
	public:
		TREENODE_DEFINE(Selector)
	};

	class RandomSelectorNodeContext : public SelectorNodeContext
	{
	protected:
		void _OnInit() override;
		RandomIndex m_RandomIndex;
	};
	class RandomSelector : public CompositeNode<RandomSelectorNodeContext>
	{
	public:
		TREENODE_DEFINE(RandomSelector)
	};
}

#endif
