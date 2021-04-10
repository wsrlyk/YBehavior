#ifndef _YBEHAVIOR_SEQUENCE_H_
#define _YBEHAVIOR_SEQUENCE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"
#include "YBehavior/runningcontext.h"

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
		friend SequenceNodeContext;
	public:
		STRING GetClassName() const override { return "Sequence"; }
		Sequence()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		IndexIterator m_Iterator;
		ContextContainer<VectorTraversalContext> m_RCContainer;
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
		STRING GetClassName() const override { return "RandomSequence"; }
		RandomSequence()
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
