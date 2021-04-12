#ifndef _YBEHAVIOR_WAIT_H_
#define _YBEHAVIOR_WAIT_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	class WaitNodeContext : public TreeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		int m_Count;

	};
	class Wait : public LeafNode<WaitNodeContext>
	{
		friend WaitNodeContext;
	public:
		STRING GetClassName() const override { return "Wait"; }
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		SharedVariableEx<INT>* m_TickCount;
	};
}

#endif
