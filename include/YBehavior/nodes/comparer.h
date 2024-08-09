#ifndef _YBEHAVIOR_COMPARER_H_
#define _YBEHAVIOR_COMPARER_H_

#include "YBehavior/treenode.h"
#include "YBehavior/operations/datacompare.h"

namespace YBehavior
{
	class Comparer : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Comparer)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		CompareType m_Operator;
		IPin* m_Opl;
		IPin* m_Opr;

		const IDataCompareHelper* m_pHelper;

	};
}

#endif
