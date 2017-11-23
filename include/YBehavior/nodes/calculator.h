#ifndef _YBEHAVIOR_CALCULATOR_H_
#define _YBEHAVIOR_CALCULATOR_H_
#endif

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	enum CalculatorOperator
	{
		CO_ADD = 1,
		CO_SUB,
		CO_MUL,
		CO_DIV
	};
	class Calculator : public BehaviorNode
	{
	public:
		Calculator();
		~Calculator();
	protected:
		virtual void OnLoaded(const pugi::xml_node& data);
		virtual NodeState Update(AgentPtr pAgent);

	private:
		CalculatorOperator m_Operator;
		ISharedVariable* m_Opl;
		ISharedVariable* m_Opr1;
		ISharedVariable* m_Opr2;

		TypeAB m_DataType;
	};
}
