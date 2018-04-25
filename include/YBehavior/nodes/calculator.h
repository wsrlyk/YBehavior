#ifndef _YBEHAVIOR_CALCULATOR_H_
#define _YBEHAVIOR_CALCULATOR_H_

#include "YBehavior/behaviortree.h"
#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	class Calculator : public LeafNode
	{
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		OperationType m_Operator;
		ISharedVariableEx* m_Opl;
		ISharedVariableEx* m_Opr1;
		ISharedVariableEx* m_Opr2;

		INT m_DataType;

	};
}

#endif
