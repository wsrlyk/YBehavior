#ifndef _YBEHAVIOR_LOOP_H_
#define _YBEHAVIOR_LOOP_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/sharedvariableex.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	enum ForPhase
	{
		FP_Normal,
		FP_Init,
		FP_Cond,
		FP_Inc,
		FP_Main,
	};

	class ForContext : public RunningContext
	{
	public:
		int LoopTimes = 0;

		ForPhase Current = FP_Normal;
	protected:
		void _OnReset() override
		{
			LoopTimes = 0;
			Current = FP_Normal;
		}
	};

	class For : public CompositeNode<>
	{
	public:
		STRING GetClassName() const override { return "For"; }
		For()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool _CheckRunningNodeState(ForPhase current, NodeState ns, int looptimes);
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(BehaviorNode * child, const STRING & connection) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		BehaviorNodePtr m_InitChild = nullptr;
		BehaviorNodePtr m_CondChild = nullptr;
		BehaviorNodePtr m_IncChild = nullptr;
		BehaviorNodePtr m_MainChild = nullptr;

		ContextContainer<ForContext> m_RCContainer;
	};

	class ForEach : public SingleChildNode<>
	{
	public:
		STRING GetClassName() const override { return "ForEach"; }
		ForEach()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		ISharedVariableEx* m_Collection = nullptr;
		ISharedVariableEx* m_Current = nullptr;
		ContextContainer<VectorTraversalContext> m_RCContainer;
	};

	class Loop : public SingleChildNode<>
	{
	public:
		STRING GetClassName() const override { return "Loop"; }
		Loop()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		SharedVariableEx<BOOL>* m_ExitWhenFailure = nullptr;
		SharedVariableEx<INT>* m_Count = nullptr;
		SharedVariableEx<INT>* m_Current = nullptr;
		ContextContainer<VectorTraversalContext> m_RCContainer;
	};

}

#endif
