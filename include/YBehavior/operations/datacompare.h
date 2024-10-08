#pragma once
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

	class IDataCompareHelper
	{
	public:
		virtual ~IDataCompareHelper() {}
	public:

		virtual bool Compare(const void* pLeft, const void* pRight, CompareType op) const = 0;
		virtual bool Compare(IMemory* pMemory, IPin* pLeft, IPin* pRight, CompareType op) const = 0;
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
	class DataCompareHelper : public IDataCompareHelper
	{
		static DataCompareHelper<T> s_Instance;
	public:
		static IDataCompareHelper* Get() { return &s_Instance; }

		bool Compare(const void* pLeft, const void* pRight, CompareType op) const override
		{
			return ValueCompare::Compare<T>(pLeft, pRight, op);
		}
		bool Compare(IMemory* pMemory, IPin* pLeft, IPin* pRight, CompareType op) const override
		{
			return ValueCompare::Compare<T>(pLeft->GetValuePtr(pMemory), pRight->GetValuePtr(pMemory), op);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class DataCompareMgr : public Singleton<DataCompareMgr>
	{
		small_map<TYPEID, const IDataCompareHelper*> m_Compares;
	public:
		DataCompareMgr();
		~DataCompareMgr();

		const IDataCompareHelper* Get(TYPEID t) const;

		template<typename T>
		const IDataCompareHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
	};
}
