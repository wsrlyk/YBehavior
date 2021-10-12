#ifndef _YBEHAVIOR_VARIABLEOPERATION_H_
#define _YBEHAVIOR_VARIABLEOPERATION_H_

#define YB_ABOUT_EQUAL_FLOAT

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

	class IVariableOperationHelper;
	struct TempObject
	{
		void* pData{ nullptr };
		IVariableOperationHelper* pHelper{ nullptr };

		TempObject(void* data, IVariableOperationHelper* helper)
			: pData(data), pHelper(helper)
		{}
		TempObject(TempObject&& o)
		{
			pData = o.pData;
			o.pData = nullptr;
		}
		~TempObject();

	private:
		TempObject(const TempObject& o) = delete;
		TempObject& operator=(const TempObject& o) = delete;
	};

	class IVariableOperationHelper
	{
	public:
		static Bimap<OperationType, STRING, EnumClassHash> s_OperatorMap;
		virtual bool Compare(const void* pLeft, const void* pRight, OperationType op) = 0;
		virtual bool Compare(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op) = 0;
		virtual void Calculate(void* pLeft, const void* pRight0, const void* pRight1, OperationType op) = 0;
		virtual const void* Calculate(const void* pRight0, const void* pRight1, OperationType op) = 0;
		virtual void Calculate(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op) = 0;
		virtual void Random(void* pLeft, const void* pRight0, const void* pRight1) = 0;
		virtual void Random(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1) = 0;
		virtual void Set(void* pLeft, const void* pRight0) = 0;
		virtual void* AllocData() = 0;
		virtual TempObject AllocTempData() = 0;
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

		template<typename T>
		static void _Add(const T& left, const T& right, T& output);
		template<typename T>
		static void _Sub(const T& left, const T& right, T& output);
		template<typename T>
		static void _Mul(const T& left, const T& right, T& output);
		template<typename T>
		static void _Div(const T& left, const T& right, T& output);
	};

	template<typename T>
	bool ValueHandler::Compare(const void* pLeft, const void* pRight, OperationType op)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in Compare" << ERROR_END;
			return false;
		}
		if (pRight == nullptr)
		{
			ERROR_BEGIN << "pRight is null in Compare" << ERROR_END;
			return false;
		}

		const T& left = *((const T*)pLeft);
		const T& right = *((const T*)pRight);

		////STRING strOp;
		////IVariableOperationHelper::s_OperatorMap.TryGetValue(op, strOp);
		////LOG_BEGIN << left << " " << strOp << " " << right << " ?" << LOG_END;

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

		////STRING strOp;
		////IVariableOperationHelper::s_OperatorMap.TryGetValue(op, strOp);

		const T& right0 = *((const T*)pRight0);
		const T& right1 = *((const T*)pRight1);

		////LOG_BEGIN << right0 << " " << strOp << " " << right1;

		T& left = *((T*)pLeft);
		switch (op)
		{
		case YBehavior::OT_ADD:
			_Add(right0, right1, left);
			//left = right0 + right1;
			break;
		case YBehavior::OT_SUB:
			_Sub(right0, right1, left);
			//left = right0 - right1;
			break;
		case YBehavior::OT_MUL:
			_Mul(right0, right1, left);
			//left = right0 * right1;
			break;
		case YBehavior::OT_DIV:
			_Div(right0, right1, left);
			//left = right0 / right1;
			break;
		default:
			return;
		}

		////LOG_BEGIN << " => " << left << LOG_END;
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

		////LOG_BEGIN << right0 << " " << right1;

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

		////LOG_BEGIN << " => " << left << LOG_END;
	}

	template<typename T>
	void ValueHandler::_Add(const T& left, const T& right, T& output)
	{
		output = left + right;
	}

	template<typename T>
	void ValueHandler::_Sub(const T& left, const T& right, T& output)
	{
		output = left - right;
	}

	template<typename T>
	void ValueHandler::_Mul(const T& left, const T& right, T& output)
	{
		output = left * right;
	}

	template<typename T>
	void ValueHandler::_Div(const T& left, const T& right, T& output)
	{
		output = left / right;
	}

	template<>
	void ValueHandler::Calculate<EntityWrapper>(void* pLeft, const void* pRight0, const void* pRight1, OperationType op);

	template<>
	void ValueHandler::Calculate<Bool>(void* pLeft, const void* pRight0, const void* pRight1, OperationType op);

#ifdef YB_ABOUT_EQUAL_FLOAT
	template<>
	bool ValueHandler::Compare<Float>(const void* pLeft, const void* pRight, OperationType op);
#endif

	template<>
	void ValueHandler::Random<Int>(void* pLeft, const void* pRight0, const void* pRight1);

	template<>
	void ValueHandler::Random<Float>(void* pLeft, const void* pRight0, const void* pRight1);

	template<>
	void ValueHandler::Random<Bool>(void* pLeft, const void* pRight0, const void* pRight1);

	template<>
	void ValueHandler::_Sub<String>(const String& left, const String& right, String& output);
	template<>
	void ValueHandler::_Mul<String>(const String& left, const String& right, String& output);
	template<>
	void ValueHandler::_Div<String>(const String& left, const String& right, String& output);

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class VariableOperationHelper : public IVariableOperationHelper
	{
		static VariableOperationHelper<T> s_Instance;
	public:
		static IVariableOperationHelper* Get() { return &s_Instance; }

		bool Compare(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
		{
			return ValueHandler::Compare<T>(pLeft->GetValue(pMemory), pRight->GetValue(pMemory), op);
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

		void Calculate(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
		{
			T left;
			ValueHandler::Calculate<T>(&left, pRight0->GetValue(pMemory), pRight1->GetValue(pMemory), op);
			pLeft->SetValue(pMemory, &left);
		}

		void Random(void* pLeft, const void* pRight0, const void* pRight1)
		{
			ValueHandler::Random<T>(pLeft, pRight0, pRight1);
		}

		void Random(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
		{
			T left;
			ValueHandler::Random<T>(&left, pRight0->GetValue(pMemory), pRight1->GetValue(pMemory));
			pLeft->SetValue(pMemory, &left);
		}

		void Set(void* pLeft, const void* pRight0)
		{
			ValueHandler::Set<T>(pLeft, pRight0);
		}

		TempObject AllocTempData()
		{
			return TempObject(ObjectPoolStatic<T>::Get(), this);
		}

		void* AllocData()
		{
			return ObjectPoolStatic<T>::Get();
		}

		void RecycleData(void* pData)
		{
			if (pData != nullptr)
				ObjectPoolStatic<T>::Recycle((T*)pData);
		}

	};
	template<typename T> VariableOperationHelper<T> VariableOperationHelper<T>::s_Instance;

	///> vector
	template<typename elementType>
	class VariableOperationHelper<StdVector<elementType>> : public IVariableOperationHelper
	{
		static VariableOperationHelper<StdVector<elementType>> s_Instance;
	public:
		static IVariableOperationHelper* Get() { return &s_Instance; }

		bool Compare(const void* pLeftValue, const void* pRightValue, OperationType op)
		{
			return false;
		}

		bool Compare(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, OperationType op)
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

		void Calculate(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, OperationType op)
		{
		}

		void Random(void* pLeft, const void* pRight0, const void* pRight1)
		{
		}

		void Random(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1)
		{
		}

		void Set(void* pLeft, const void* pRight0)
		{
			ValueHandler::Set<StdVector<elementType>>(pLeft, pRight0);
		}

		TempObject AllocTempData()
		{
			return TempObject(ObjectPoolStatic<StdVector<elementType>>::Get(), this);
		}

		void* AllocData()
		{
			return ObjectPoolStatic<StdVector<elementType>>::Get();
		}
		void RecycleData(void* pData)
		{
			if (pData != nullptr)
				ObjectPoolStatic<StdVector<elementType>>::Recycle((StdVector<elementType>*)pData);
		}
	};
	template<typename elementType> VariableOperationHelper<StdVector<elementType>> VariableOperationHelper<StdVector<elementType>>::s_Instance;

}

#endif
