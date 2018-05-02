#ifndef _YBEHAVIOR_COMPARER_H_
#define _YBEHAVIOR_COMPARER_H_

#include "YBehavior/behaviortree.h"
#include <unordered_set>
#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	class Comparer : public LeafNode
	{
	public:
		STRING GetClassName() const override { return "Comparer"; }
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		OperationType m_Operator;
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr;

		INT m_DataType;

	};
}

#endif
