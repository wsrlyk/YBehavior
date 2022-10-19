#ifndef _YBEHAVIOR_VARIABLECOMPARE_H_
#define _YBEHAVIOR_VARIABLECOMPARE_H_

#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/logger.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	enum struct CompareType
	{
		EQUAL,
		NOT_EQUAL,
		GREATER,
		LESS,
		LESS_EQUAL,
		GREATER_EQUAL,
	};

	class IVariableCompareHelper
	{
	public:
		virtual ~IVariableCompareHelper() {}
	public:

		virtual bool Compare(const void* pLeft, const void* pRight, CompareType op) const = 0;
		virtual bool Compare(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, CompareType op) const = 0;
	};

	//struct NormalCalculateTag {};
	//struct InvalidCalculateTag {};
	//template<typename T> struct CalculateTagDispatchTrait {
	//	using Tag = NormalCalculateTag;
	//};
	//template<> struct CalculateTagDispatchTrait<EntityWrapper> {
	//	using Tag = InvalidCalculateTag;
	//};

	class ValueCompare
	{
	public:
		template<typename T>
		static bool Compare(const void* pLeft, const void* pRight, CompareType op)
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
			case CompareType::EQUAL:
				return left == right;
			case CompareType::NOT_EQUAL:
				return left != right;
			case CompareType::GREATER:
				return left > right;
			case CompareType::LESS:
				return left < right;
			case CompareType::LESS_EQUAL:
				return left <= right;
			case CompareType::GREATER_EQUAL:
				return left >= right;
			default:
				return false;
			}
		}
	};

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class VariableCompareHelper : public IVariableCompareHelper
	{
		static VariableCompareHelper<T> s_Instance;
	public:
		static IVariableCompareHelper* Get() { return &s_Instance; }

		bool Compare(const void* pLeft, const void* pRight, CompareType op) const override
		{
			return ValueCompare::Compare<T>(pLeft, pRight, op);
		}
		bool Compare(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight, CompareType op) const override
		{
			return ValueCompare::Compare<T>(pLeft->GetValue(pMemory), pRight->GetValue(pMemory), op);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class VariableCompareMgr : public Singleton<VariableCompareMgr>
	{
		small_map<TYPEID, const IVariableCompareHelper*> m_Compares;
	public:
		VariableCompareMgr();
		~VariableCompareMgr();

		const IVariableCompareHelper* Get(TYPEID t) const;

		template<typename T>
		const IVariableCompareHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
	};
}

#endif
