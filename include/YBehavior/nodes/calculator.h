#ifndef _YBEHAVIOR_CALCULATOR_H_
#define _YBEHAVIOR_CALCULATOR_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	class Calculator : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Calculator)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		OperationType m_Operator;
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr1;
		ISharedVariableEx* m_Opr2;

		TYPEID m_DataType;

	};
}

#endif
