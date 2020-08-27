#ifndef _YBEHAVIOR_IFTHENELSE_H_
#define _YBEHAVIOR_IFTHENELSE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	enum IfThenElsePhase
	{
		ITE_Normal,
		ITE_If,
		ITE_Then,
		ITE_Else,
	};

	class IfThenElseContext : public RunningContext
	{
	public:
		IfThenElsePhase Current = ITE_Normal;
	protected:
		void _OnReset() override
		{
			Current = ITE_Normal;
		}
	};

	class IfThenElse : public BranchNode
	{
	public:
		STRING GetClassName() const override { return "IfThenElse"; }
	public:
		IfThenElse();
		~IfThenElse();
	protected:
		NodeState Update(AgentPtr pAgent) override;
		void OnAddChild(BehaviorNode* child, const STRING& connection) override;
		bool _CheckRunningNodeState(IfThenElsePhase current, NodeState ns);

		BehaviorNode* m_If;
		BehaviorNode* m_Then;
		BehaviorNode* m_Else;
		ContextContainer<IfThenElseContext> m_RCContainer;
	};
}

#endif
