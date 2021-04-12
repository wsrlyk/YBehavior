#ifndef _YBEHAVIOR_LOOP_H_
#define _YBEHAVIOR_LOOP_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	enum struct ForPhase
	{
		None,
		Init,
		Cond,
		Inc,
		Main,
	};

	class ForNodeContext : public CompositeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		int m_LoopTimes;
	};

	class For : public CompositeNode<ForNodeContext>
	{
		friend ForNodeContext;
	public:
		STRING GetClassName() const override { return "For"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		BehaviorNodePtr m_InitChild = nullptr;
		BehaviorNodePtr m_CondChild = nullptr;
		BehaviorNodePtr m_IncChild = nullptr;
		BehaviorNodePtr m_MainChild = nullptr;
	};

	//////////////////////////////////////////////////////////////////////////
	class ForEachNodeContext : public SingleChildNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class ForEach : public SingleChildNode<ForEachNodeContext>
	{
		friend ForEachNodeContext;
	public:
		STRING GetClassName() const override { return "ForEach"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		ISharedVariableEx* m_Collection = nullptr;
		ISharedVariableEx* m_Current = nullptr;
	};

	//////////////////////////////////////////////////////////////////////////

	class LoopNodeContext : public SingleChildNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class Loop : public SingleChildNode<LoopNodeContext>
	{
		friend LoopNodeContext;
	public:
		STRING GetClassName() const override { return "Loop"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		SharedVariableEx<INT>* m_Count = nullptr;
		SharedVariableEx<INT>* m_Current = nullptr;
	};

}

#endif
