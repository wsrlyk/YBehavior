#ifndef _YBEHAVIOR_DICE_H_
#define _YBEHAVIOR_DICE_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IVariableCompareHelper;
	class IVariableCalculateHelper;
	class IVariableOperationHelper;
	class IVariableRandomHelper;
	class Dice : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Dice)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		ISharedVariableEx* m_Distribution;
		ISharedVariableEx* m_Values;

		ISharedVariableEx* m_Input;
		ISharedVariableEx* m_Output;

		const IVariableCompareHelper* m_pCompareHelper;
		const IVariableCalculateHelper* m_pCalculateHelper;
		const IVariableOperationHelper* m_pOperationHelper;
		const IVariableRandomHelper* m_pRandomHelper;
	};
}

#endif
