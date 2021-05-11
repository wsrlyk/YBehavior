#ifndef _YBEHAVIOR_COMPARER_H_
#define _YBEHAVIOR_COMPARER_H_

#include "YBehavior/treenode.h"
#include <unordered_set>
#include "YBehavior/variableoperation.h"

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
		OperationType m_Operator;
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr;

		TYPEID m_DataType;

	};
}

#endif
