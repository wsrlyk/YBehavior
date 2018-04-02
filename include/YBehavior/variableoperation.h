#ifndef _YBEHAVIOR_VARIABLEOPERATION_H_
#define _YBEHAVIOR_VARIABLEOPERATION_H_

#include "YBehavior/interface.h"
#include "YBehavior/tools/bimap.h"
#include "YBehavior/types.h"

namespace YBehavior
{
	enum OperationType
	{
		OT_EQUAL,
		OT_NOT_EQUAL,
		OT_GREATER,
		OT_LESS,
		OT_LESS_EQUAL,
		OT_GREATER_EQUAL,

		OT_ADD,
		OT_SUB,
		OT_MUL,
		OT_DIV
	};

	class IVariableOperationHelper
	{
	public:
		static Bimap<OperationType, STRING, EnumClassHash> s_OperatorMap;

		virtual bool Compare(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op) = 0;
		virtual void Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op) = 0;
	};

	class ValueHandler
	{
	public:
		template<typename T>
		static bool Compare(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op);

		template<typename T>
		static void Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op);
	};

	template<typename T>
	bool ValueHandler::Compare(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
	{
		const T& left = *((const T*)pLeft->GetValue(pData));
		const T& right = *((const T*)pRight->GetValue(pData));

		STRING strOp;
		IVariableOperationHelper::s_OperatorMap.TryGetValue(op, strOp);
		LOG_BEGIN << left << " " << strOp << " " << right << " ?" << LOG_END;

		switch (op)
		{
		case YBehavior::OT_EQUAL:
			return left == right;
			break;
		case YBehavior::OT_NOT_EQUAL:
			return left != right;
			break;
		case YBehavior::OT_GREATER:
			return left > right;
			break;
		case YBehavior::OT_LESS:
			return left < right;
			break;
		case YBehavior::OT_LESS_EQUAL:
			return left <= right;
			break;
		case YBehavior::OT_GREATER_EQUAL:
			return left >= right;
			break;
		default:
			return false;
			break;
		}
	}

	template<typename T>
	void ValueHandler::Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
	{
		STRING strOp;
		IVariableOperationHelper::s_OperatorMap.TryGetValue(op, strOp);

		const T& right0 = *((const T*)pRight0->GetValue(pData));
		const T& right1 = *((const T*)pRight1->GetValue(pData));

		LOG_BEGIN << right0 << " " << strOp << " " << right1;

		T left;
		switch (op)
		{
		case YBehavior::OT_ADD:
			left = right0 + right1;
			break;
		case YBehavior::OT_SUB:
			left = right0 - right1;
			break;
		case YBehavior::OT_MUL:
			left = right0 * right1;
			break;
		case YBehavior::OT_DIV:
			left = right0 / right1;
			break;
		default:
			return;
		}

		pLeft->SetValue(pData, &left);

		LOG_BEGIN << "=>" << left << LOG_END;
	}

	template<>
	bool ValueHandler::Compare<AgentWrapper>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op);

	template<>
	static void ValueHandler::Calculate<String>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op);

	template<>
	static void ValueHandler::Calculate<AgentWrapper>(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op);

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class VariableOperationHelper : public IVariableOperationHelper
	{
		static VariableOperationHelper<T> s_Instance;
	public:
		static IVariableOperationHelper* Get() { return &s_Instance; }

		bool Compare(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
		{
			return ValueHandler::Compare<T>(pData, pLeft, pRight, op);
		}

		void Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
		{
			return ValueHandler::Calculate<T>(pData, pLeft, pRight0, pRight1, op);
		}
	};
	template<typename T> VariableOperationHelper<T> VariableOperationHelper<T>::s_Instance;
}

#endif