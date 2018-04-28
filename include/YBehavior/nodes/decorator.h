#ifndef _YBEHAVIOR_DECORATOR_H_
#define _YBEHAVIOR_DECORATOR_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class YBEHAVIOR_API AlwaysSuccess : public SingleChildNode
	{
	public:
		STRING GetName() const override { return "AlwaysSuccess"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
	class YBEHAVIOR_API AlwaysFailed : public SingleChildNode
	{
	public:
		STRING GetName() const override { return "AlwaysFailed"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
	class YBEHAVIOR_API Invertor : public SingleChildNode
	{
	public:
		STRING GetName() const override { return "Invertor"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
	};
}

#endif
