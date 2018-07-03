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
		virtual NodeState Update(AgentPtr pAgent);
		virtual void OnLoaded(const pugi::xml_node& data);

		ISharedVariableEx* m_Distribution;
		ISharedVariableEx* m_Values;

		ISharedVariableEx* m_Input;
		ISharedVariableEx* m_Output;

		SharedVariableEx<BOOL>* m_IgnoreInput;
	};
}

#endif
