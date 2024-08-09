#ifndef _YBEHAVIOR_DICE_H_
#define _YBEHAVIOR_DICE_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IDataCompareHelper;
	class IDataCalculateHelper;
	class IDataOperationHelper;
	class IDataRandomHelper;
	class Dice : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Dice)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		IPin* m_Distribution;
		IPin* m_Values;

		IPin* m_Input;
		IPin* m_Output;

		const IDataCompareHelper* m_pCompareHelper;
		const IDataCalculateHelper* m_pCalculateHelper;
		const IDataOperationHelper* m_pOperationHelper;
		const IDataRandomHelper* m_pRandomHelper;
	};
}

#endif
