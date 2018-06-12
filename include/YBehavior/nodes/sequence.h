#ifndef _YBEHAVIOR_SEQUENCE_H_
#define _YBEHAVIOR_SEQUENCE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class Sequence : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "Sequence"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
	};

	class RandomSequence : public CompositeNode
	{
	public:
		STRING GetClassName() const override { return "RandomSequence"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;
		RandomIndex m_RandomIndex;
	};

}

#endif
