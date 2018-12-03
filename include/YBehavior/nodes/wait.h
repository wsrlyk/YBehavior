#ifndef _YBEHAVIOR_WAIT_H_
#define _YBEHAVIOR_WAIT_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/runningcontext.h"

namespace YBehavior
{
	class WaitContext : public RunningContext
	{
	public:
		int Current = 0;
	protected:
		void _OnReset() override
		{
			Current = 0;
		}
	};
	class Wait : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "Wait"; }
		Wait()
		{
			SetRCCreator(&m_RCContainer);
		}
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual bool OnLoaded(const pugi::xml_node& data);

	private:
		SharedVariableEx<INT>* m_TickCount;
		ContextContainer<WaitContext> m_RCContainer;
	};
}

#endif
