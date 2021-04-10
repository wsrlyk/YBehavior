#ifndef _YBEHAVIOR_PIECEWISEFUNCTION_H_
#define _YBEHAVIOR_PIECEWISEFUNCTION_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class PiecewiseFunction : public LeafNode<>
	{
	public:
		STRING GetClassName() const override { return "PiecewiseFunction"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		ISharedVariableEx* m_KeyPointX;
		ISharedVariableEx* m_KeyPointY;

		ISharedVariableEx* m_InputX;
		ISharedVariableEx* m_OutputY;
	};
}

#endif
