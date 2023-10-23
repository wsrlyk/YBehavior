#ifndef _YBEHAVIOR_VARIABLESETOPERATION_H_
#define _YBEHAVIOR_VARIABLESETOPERATION_H_

#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/logger.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	enum struct ArrayOperationType
	{
		APPEND,
		MERGE,
		EXCLUDE,
	};

	class IVariableArrayOperationHelper
	{
	public:
		virtual ~IVariableArrayOperationHelper() {}
	public:
		virtual void ArrayOperation(void* pLeft, const void* pRight0, const void* pRight1, ArrayOperationType op) const = 0;
		virtual const void* ArrayOperation(const void* pRight0, const void* pRight1, ArrayOperationType op) const = 0;
		virtual void ArrayOperation(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, ArrayOperationType op) const = 0;
	};

	class ValueArrayOperation
	{
	public:
		template<typename T>
		static void ArrayOperation(void* pLeft, const void* pRight0, const void* pRight1, ArrayOperationType op)
		{
			return _DoArrayOperation<T>(pLeft, pRight0, pRight1, op/*, typename CalculateTagDispatchTrait<T>::Tag{}*/);
		}

	private:
		template<typename T>
		static void _DoArrayOperation(void* pLeft, const void* pRight0, const void* pRight1, ArrayOperationType op/*, NormalCalculateTag*/);

		template<typename T>
		static void _Append(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output);
		template<typename T>
		static void _Merge(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output);
		template<typename T>
		static void _Exclude(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output);
	};

	template<typename T>
	void ValueArrayOperation::_DoArrayOperation(void* pLeft, const void* pRight0, const void* pRight1, ArrayOperationType op/*, NormalCalculateTag*/)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in SetOperation" << ERROR_END;
			return;
		}
		if (pRight0 == nullptr)
		{
			ERROR_BEGIN << "pRight0 is null in SetOperation" << ERROR_END;
			return;
		}
		if (pRight1 == nullptr)
		{
			ERROR_BEGIN << "pRight1 is null in SetOperation" << ERROR_END;
			return;
		}

		const T& right0 = *((const T*)pRight0);
		const T& right1 = *((const T*)pRight1);

		T& left = *((T*)pLeft);
		switch (op)
		{
		case YBehavior::ArrayOperationType::APPEND:
			_Append(right0, right1, left);
			break;
		case YBehavior::ArrayOperationType::MERGE:
			_Merge(right0, right1, left);
			break;
		case YBehavior::ArrayOperationType::EXCLUDE:
			_Exclude(right0, right1, left);
			break;
		default:
			return;
		}
	}

	template<typename T>
	void ValueArrayOperation::_Append(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output)
	{
		if (&output == &left)
		{
			output.insert(output.end(), right.begin(), right.end());
		}
		else if (&output == &right)
		{
			output.insert(output.begin(), left.begin(), left.end());
		}
		else
		{
			output.clear();
			output.insert(output.end(), left.begin(), left.end());
			output.insert(output.end(), right.begin(), right.end());
		}
	}

	template<typename T>
	void ValueArrayOperation::_Merge(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output)
	{
		if (&output == &left)
		{
			output.insert(output.end(), right.begin(), right.end());
		}
		else if (&output == &right)
		{
			output.insert(output.end(), left.begin(), left.end());
		}
		else
		{
			output.clear();
			output.insert(output.end(), left.begin(), left.end());
			output.insert(output.end(), right.begin(), right.end());
		}
		std::sort(output.begin(), output.end());
		auto res = std::unique(output.begin(), output.end());
		if (res != output.end())
			output.erase(res, output.end());
	}

	template<typename T>
	void ValueArrayOperation::_Exclude(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output)
	{
	//TODO
	}

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class VariableArrayOperationHelper : public IVariableArrayOperationHelper
	{
		static VariableArrayOperationHelper<T> s_Instance;
	public:
		static IVariableArrayOperationHelper* Get() { return &s_Instance; }

		void ArrayOperation(void* pLeft, const void* pRight0, const void* pRight1, ArrayOperationType op) const override
		{
			ValueArrayOperation::ArrayOperation<T>(pLeft, pRight0, pRight1, op);
		}
		const void* ArrayOperation(const void* pRight0, const void* pRight1, ArrayOperationType op) const override
		{
			T& left = PreservedValue<T>::Data;
			ValueArrayOperation::ArrayOperation<T>(&left, pRight0, pRight1, op);
			return &left;
		}
		void ArrayOperation(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, ArrayOperationType op) const override
		{
			auto left =const_cast<void*>(pLeft->GetValue(pMemory));
			ValueArrayOperation::ArrayOperation<T>(left, pRight0->GetValue(pMemory), pRight1->GetValue(pMemory), op);
			//pLeft->SetValue(pMemory, &left);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class VariableArrayOperationMgr : public Singleton<VariableArrayOperationMgr>
	{
		/// <summary>
		/// Left, Right0, Right1
		/// </summary>
		small_map<TYPEID, const IVariableArrayOperationHelper*> m_Operations;
	public:
		VariableArrayOperationMgr();
		~VariableArrayOperationMgr();

		const IVariableArrayOperationHelper* Get(TYPEID t) const;

		template<typename T>
		const IVariableArrayOperationHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
		template<typename TL, typename TR0, typename TR1>
		const IVariableArrayOperationHelper* Get() const
		{
			return Get(GetTypeID<TL>(), GetTypeID<TR0>(), GetTypeID<TR0>());
		}
	};
}

#endif
