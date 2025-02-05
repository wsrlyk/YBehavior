#ifndef _YBEHAVIOR_SEQUENCE_H_
#define _YBEHAVIOR_SEQUENCE_H_

#include "YBehavior/treenode.h"
#include "YBehavior/tools/common.h"
namespace YBehavior
{
	class SequenceNodeContext : public CompositeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		IndexIterator m_Iterator;
	};
	class Sequence : public CompositeNode<SequenceNodeContext>
	{
	public:
		TREENODE_DEFINE(Sequence)
	};

	class RandomSequenceNodeContext : public SequenceNodeContext
	{
	protected:
		void _OnInit() override;
		RandomIndex m_RandomIndex;
	};
	class RandomSequence : public CompositeNode<RandomSequenceNodeContext>
	{
	public:
		TREENODE_DEFINE(RandomSequence)
	protected:
	};

}

#endif
