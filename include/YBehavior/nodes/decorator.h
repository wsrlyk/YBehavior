#ifndef _YBEHAVIOR_DECORATOR_H_
#define _YBEHAVIOR_DECORATOR_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class AlwaysSuccessNodeContext : public SingleChildNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};
	class AlwaysSuccess : public SingleChildNode<AlwaysSuccessNodeContext>
	{
	public:
		TREENODE_DEFINE(AlwaysSuccess)
	protected:
	};
	//////////////////////////////////////////////////////////////////////////
	class AlwaysFailureNodeContext : public SingleChildNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};
	class AlwaysFailure : public SingleChildNode<AlwaysFailureNodeContext>
	{
	public:
		TREENODE_DEFINE(AlwaysFailure)
	protected:
	};

	///> Never need anymore
	//class Invertor : public SingleChildNode<>
	//{
	//public:
	//	STRING GetClassName() const override { return "Invertor"; }
	//protected:
	//	NodeState Update(AgentPtr pAgent) override;
	//};
}

#endif
