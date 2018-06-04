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
	template<>
	void ValueHandler::Calculate<Bool>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
	{

	}

	template<>
	void ValueHandler::Random<Int>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
	{
		_DoRandom<Int>(pData, pLeft, pRight0, pRight1);
	}

	template<>
	void ValueHandler::Random<Float>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
	{
		_DoRandom<Float>(pData, pLeft, pRight0, pRight1);
	}

	template<>
	void ValueHandler::Random<Bool>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
	{
		_DoRandom<Bool>(pData, pLeft, pRight0, pRight1);
	}

}
