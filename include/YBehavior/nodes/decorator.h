#ifndef _YBEHAVIOR_DECORATOR_H_
#define _YBEHAVIOR_DECORATOR_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class AlwaysSuccess : public SingleChildNode<>
	{
	public:
		STRING GetClassName() const override { return "AlwaysSuccess"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
	class AlwaysFailure : public SingleChildNode<>
	{
	public:
		STRING GetClassName() const override { return "AlwaysFailure"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
	class Invertor : public SingleChildNode<>
	{
	public:
		STRING GetClassName() const override { return "Invertor"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
}

#endif
