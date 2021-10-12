#ifndef _YBEHAVIOR_CALCULATOR_H_
#define _YBEHAVIOR_CALCULATOR_H_

#include "YBehavior/treenode.h"
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
		ISharedVariableEx* m_Output;
		ISharedVariableEx* m_Input1;
		ISharedVariableEx* m_Input2;

		TYPEID m_DataType;

	};
}

#endif
