#ifndef _YBEHAVIOR_PIECEWISEFUNCTION_H_
#define _YBEHAVIOR_PIECEWISEFUNCTION_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IVariableCompareHelper;
	class IVariableCalculateHelper;
	class IVariableOperationHelper;
	class PiecewiseFunction : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(PiecewiseFunction)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		ISharedVariableEx* m_KeyPointX;
		ISharedVariableEx* m_KeyPointY;

		ISharedVariableEx* m_InputX;
		ISharedVariableEx* m_OutputY;

		const IVariableCompareHelper* m_pCompareHelper;
		const IVariableCalculateHelper* m_pCalculateHelper;
		const IVariableOperationHelper* m_pOperationHelper;
	};
}

#endif
