#ifndef _YBEHAVIOR_VARIABLESETOPERATION_H_
#define _YBEHAVIOR_VARIABLESETOPERATION_H_

#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/logger.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	enum struct SetOperationType
	{
		APPEND,
		MERGE,
		EXCLUDE,
	};

	class IVariableSetOperationHelper
	{
	public:
		virtual ~IVariableSetOperationHelper() {}
	public:
		virtual void SetOperation(void* pLeft, const void* pRight0, const void* pRight1, SetOperationType op) const = 0;
		virtual const void* SetOperation(const void* pRight0, const void* pRight1, SetOperationType op) const = 0;
		virtual void SetOperation(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, SetOperationType op) const = 0;
	};

	class ValueSetOperation
	{
	public:
		template<typename T>
		static void SetOperation(void* pLeft, const void* pRight0, const void* pRight1, SetOperationType op)
		{
			return _DoSetOperation<T>(pLeft, pRight0, pRight1, op/*, typename CalculateTagDispatchTrait<T>::Tag{}*/);
		}

	private:
		template<typename T>
		static void _DoSetOperation(void* pLeft, const void* pRight0, const void* pRight1, SetOperationType op/*, NormalCalculateTag*/);

		template<typename T>
		static void _Append(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output);
		template<typename T>
		static void _Merge(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output);
		template<typename T>
		static void _Exclude(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output);
	};

	template<typename T>
	void ValueSetOperation::_DoSetOperation(void* pLeft, const void* pRight0, const void* pRight1, SetOperationType op/*, NormalCalculateTag*/)
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
		case YBehavior::SetOperationType::APPEND:
			_Append(right0, right1, left);
			break;
		case YBehavior::SetOperationType::MERGE:
			_Merge(right0, right1, left);
			break;
		case YBehavior::SetOperationType::EXCLUDE:
			_Exclude(right0, right1, left);
			break;
		default:
			return;
		}
	}

	template<typename T>
	void ValueSetOperation::_Append(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output)
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
	void ValueSetOperation::_Merge(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output)
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
	void ValueSetOperation::_Exclude(const StdVector<T>& left, const StdVector<T>& right, StdVector<T>& output)
	{
	//TODO
	}

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class VariableSetOperationHelper : public IVariableSetOperationHelper
	{
		static VariableSetOperationHelper<T> s_Instance;
	public:
		static IVariableSetOperationHelper* Get() { return &s_Instance; }

		void SetOperation(void* pLeft, const void* pRight0, const void* pRight1, SetOperationType op) const override
		{
			ValueSetOperation::SetOperation<T>(pLeft, pRight0, pRight1, op);
		}
		const void* SetOperation(const void* pRight0, const void* pRight1, SetOperationType op) const override
		{
			T& left = PreservedValue<T>::Data;
			ValueSetOperation::SetOperation<T>(&left, pRight0, pRight1, op);
			return &left;
		}
		void SetOperation(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, SetOperationType op) const override
		{
			auto left =const_cast<void*>(pLeft->GetValue(pMemory));
			ValueSetOperation::SetOperation<T>(left, pRight0->GetValue(pMemory), pRight1->GetValue(pMemory), op);
			//pLeft->SetValue(pMemory, &left);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class VariableSetOperationMgr : public Singleton<VariableSetOperationMgr>
	{
		/// <summary>
		/// Left, Right0, Right1
		/// </summary>
		small_map<TYPEID, const IVariableSetOperationHelper*> m_SetOperations;
	public:
		VariableSetOperationMgr();
		~VariableSetOperationMgr();

		const IVariableSetOperationHelper* Get(TYPEID t) const;

		template<typename T>
		const IVariableSetOperationHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
		template<typename TL, typename TR0, typename TR1>
		const IVariableSetOperationHelper* Get() const
		{
			return Get(GetTypeID<TL>(), GetTypeID<TR0>(), GetTypeID<TR0>());
		}
	};
}

#endif
