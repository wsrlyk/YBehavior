#ifndef _YBEHAVIOR_DICE_H_
#define _YBEHAVIOR_DICE_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/tools/common.h"

namespace YBehavior
{
	class Dice : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "Dice"; }
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		ISharedVariableEx* m_Distribution;
		ISharedVariableEx* m_Values;

		ISharedVariableEx* m_Input;
		ISharedVariableEx* m_Output;

		SharedVariableEx<BOOL>* m_IgnoreInput;
	};
}

#endif
