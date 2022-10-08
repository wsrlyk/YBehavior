#ifndef _YBEHAVIOR_COMPARER_H_
#define _YBEHAVIOR_COMPARER_H_

#include "YBehavior/treenode.h"
#include <unordered_set>
#include "YBehavior/variables/variablecompare.h"

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
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr;

		const IVariableCompareHelper* m_pHelper;

	};
}

#endif
