#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	template<>
	bool ValueHandler::Compare<AgentWrapper>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
	{
		return false;
	}

	template<>
	void ValueHandler::Calculate<String>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
	{

	}
	template<>
	void ValueHandler::Calculate<AgentWrapper>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
	{

	}
}
