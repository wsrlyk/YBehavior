#ifndef _YBEHAVIOR_PIECEWISEFUNCTION_H_
#define _YBEHAVIOR_PIECEWISEFUNCTION_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IDataCompareHelper;
	class IDataCalculateHelper;
	class IDataOperationHelper;
	class PiecewiseFunction : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(PiecewiseFunction)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

		IPin* m_KeyPointX;
		IPin* m_KeyPointY;

		IPin* m_InputX;
		IPin* m_OutputY;

		const IDataCompareHelper* m_pCompareHelper;
		const IDataCalculateHelper* m_pCalculateHelper;
		const IDataOperationHelper* m_pOperationHelper;
	};
}

#endif
