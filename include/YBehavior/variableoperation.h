#ifndef _YBEHAVIOR_VARIABLEOPERATION_H_
#define _YBEHAVIOR_VARIABLEOPERATION_H_

#include "YBehavior/interface.h"
#include "YBehavior/tools/bimap.h"
#include "YBehavior/types.h"
#include "YBehavior/logger.h"
#include "utility.h"
#include "tools/objectpool.h"

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
		virtual bool Compare(const void* pLeft, const void* pRight, OperationType op) = 0;
		virtual bool Compare(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op) = 0;
		virtual void Calculate(void* pLeft, const void* pRight0, const void* pRight1, OperationType op) = 0;
		virtual const void* Calculate(const void* pRight0, const void* pRight1, OperationType op) = 0;
		virtual void Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op) = 0;
		virtual void Random(void* pLeft, const void* pRight0, const void* pRight1) = 0;
		virtual void Random(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1) = 0;
		virtual void Set(void* pLeft, const void* pRight0) = 0;
		virtual void* AllocData() = 0;
		virtual void RecycleData(void* pData) = 0;
	};


	///> Let the POINTER of the result of calculation returnable
	template<typename T>
	class PreservedValue
	{
	public:
		static T Data;
	};
	template<typename T> T PreservedValue<T>::Data;

	class ValueHandler
	{
	public:
		template<typename T>
		static bool Compare(const void* pLeft, const void* pRight, OperationType op);

		template<typename T>
		static void Calculate(void* pLeft, const void* pRight0, const void* pRight1, OperationType op);

		template<typename T>
		static void Random(void* pLeft, const void* pRight0, const void* pRight1);

		template<typename T>
		static void Set(void* pLeft, const void* pRight0);

	private:
		template<typename T>
		static void _DoRandom(void* pLeft, const void* pRight0, const void* pRight1);
	};

	template<typename T>
	bool ValueHandler::Compare(const void* pLeft, const void* pRight, OperationType op)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in Calculate" << ERROR_END;
			return false;
		}
		if (pRight == nullptr)
		{
			ERROR_BEGIN << "pRight is null in Calculate" << ERROR_END;
			return false;
		}

		const T& left = *((const T*)pLeft);
		const T& right = *((const T*)pRight);

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
	void ValueHandler::Calculate(void* pLeft, const void* pRight0, const void* pRight1, OperationType op)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in Calculate" << ERROR_END;
			return;
		}
		if (pRight0 == nullptr)
		{
			ERROR_BEGIN << "pRight0 is null in Calculate" << ERROR_END;
			return;
		}
		if (pRight1 == nullptr)
		{
			ERROR_BEGIN << "pRight1 is null in Calculate" << ERROR_END;
			return;
		}

		STRING strOp;
		IVariableOperationHelper::s_OperatorMap.TryGetValue(op, strOp);

		const T& right0 = *((const T*)pRight0);
		const T& right1 = *((const T*)pRight1);

		LOG_BEGIN << right0 << " " << strOp << " " << right1;

		T& left = *((T*)pLeft);
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

		LOG_BEGIN << " => " << left << LOG_END;
	}

	template<typename T>
	void ValueHandler::Set(void* pLeft, const void* pRight0)
	{
		T& left = *((T*)pLeft);
		left = *((T*)pRight0);
	}

	template<typename T>
	void ValueHandler::Random(void* pLeft, const void* pRight0, const void* pRight1)
	{
	}

	template<typename T>
	void ValueHandler::_DoRandom(void* pLeft, const void* pRight0, const void* pRight1)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in Random" << ERROR_END;
			return;
		}
		if (pRight0 == nullptr)
		{
			ERROR_BEGIN << "pRight0 is null in Random" << ERROR_END;
			return;
		}
		if (pRight1 == nullptr)
		{
			ERROR_BEGIN << "pRight1 is null in Random" << ERROR_END;
			return;
		}

		const T& right0 = *((const T*)pRight0);
		const T& right1 = *((const T*)pRight1);

		LOG_BEGIN << right0 << " " << right1;

		T& left = *((T*)pLeft);

		if (right0 == right1)
		{
			left = right0;
			return;
		}
		T small = right0;
		T large = right1;
		if (small > large)
		{
			T tmp = small;
			small = large;
			large = tmp;
		}
		left = Utility::Rand(small, large);

		LOG_BEGIN << " => " << left << LOG_END;
	}

	template<>
	bool ValueHandler::Compare<EntityWrapper>(const void* pLeft, const void* pRight, OperationType op);


	template<>
	void ValueHandler::Calculate<String>(void* pLeft, const void* pRight0, const void* pRight1, OperationType op);

	template<>
	void ValueHandler::Calculate<EntityWrapper>(void* pLeft, const void* pRight0, const void* pRight1, OperationType op);

	template<>
	void ValueHandler::Calculate<Bool>(void* pLeft, const void* pRight0, const void* pRight1, OperationType op);


	template<>
	void ValueHandler::Random<Int>(void* pLeft, const void* pRight0, const void* pRight1);

	template<>
	void ValueHandler::Random<Float>(void* pLeft, const void* pRight0, const void* pRight1);

	template<>
	void ValueHandler::Random<Bool>(void* pLeft, const void* pRight0, const void* pRight1);

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
			return ValueHandler::Compare<T>(pLeft->GetValue(pData), pRight->GetValue(pData), op);
		}

		bool Compare(const void* pLeftValue, const void* pRightValue, OperationType op)
		{
			return ValueHandler::Compare<T>(pLeftValue, pRightValue, op);
		}

		void Calculate(void* pLeft, const void* pRight0, const void* pRight1, OperationType op)
		{
			ValueHandler::Calculate<T>(pLeft, pRight0, pRight1, op);
		}

		const void* Calculate(const void* pRight0, const void* pRight1, OperationType op)
		{
			T& left = PreservedValue<T>::Data;
			ValueHandler::Calculate<T>(&left, pRight0, pRight1, op);
			return &left;
		}

		void Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
		{
			T left;
			ValueHandler::Calculate<T>(&left, pRight0->GetValue(pData), pRight1->GetValue(pData), op);
			pLeft->SetValue(pData, &left);
		}

		void Random(void* pLeft, const void* pRight0, const void* pRight1)
		{
			ValueHandler::Random<T>(pLeft, pRight0, pRight1);
		}

		void Random(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
		{
			T left;
			ValueHandler::Random<T>(&left, pRight0->GetValue(pData), pRight1->GetValue(pData));
			pLeft->SetValue(pData, &left);
		}

		void Set(void* pLeft, const void* pRight0)
		{
			ValueHandler::Set<T>(pLeft, pRight0);
		}

		void* AllocData()
		{
			return ObjectPool<T>::Get();
		}
		void RecycleData(void* pData)
		{
			if (pData != nullptr)
				ObjectPool<T>::Recycle((T*)pData);
		}

	};
	template<typename T> VariableOperationHelper<T> VariableOperationHelper<T>::s_Instance;

	///> Exclude vector
	template<typename elementType>
	class VariableOperationHelper<std::vector<elementType>> : public IVariableOperationHelper
	{
		static VariableOperationHelper<std::vector<elementType>> s_Instance;
	public:
		static IVariableOperationHelper* Get() { return &s_Instance; }

		bool Compare(const void* pLeftValue, const void* pRightValue, OperationType op)
		{
			return false;
		}

		bool Compare(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
		{
			return false;
		}

		const void* Calculate(const void* pRight0, const void* pRight1, OperationType op)
		{
			return nullptr;
		}

		void Calculate(void* pLeft, const void* pRight0, const void* pRight1, OperationType op)
		{
		}

		void Calculate(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
		{
		}

		void Random(void* pLeft, const void* pRight0, const void* pRight1)
		{
		}

		void Random(SharedDataEx* pData, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
		{
		}

		void Set(void* pLeft, const void* pRight0)
		{
			ValueHandler::Set<std::vector<elementType>>(pLeft, pRight0);
		}

		void* AllocData()
		{
			return ObjectPool<std::vector<elementType>>::Get();
		}
		void RecycleData(void* pData)
		{
			if (pData != nullptr)
				ObjectPool<std::vector<elementType>>::Recycle((std::vector<elementType>*)pData);
		}
	};
	template<typename elementType> VariableOperationHelper<std::vector<elementType>> VariableOperationHelper<std::vector<elementType>>::s_Instance;

}

#endif
