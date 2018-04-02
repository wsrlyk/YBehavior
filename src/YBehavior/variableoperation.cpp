#include "YBehavior/variableoperation.h"

namespace YBehavior
{
	template<>
	bool ValueHandler::Compare<AgentWrapper>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
	{
		return false;
	}

	template<>
	static void ValueHandler::Calculate<String>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
	{

	}
	template<>
	static void ValueHandler::Calculate<AgentWrapper>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
	{

	}
}
