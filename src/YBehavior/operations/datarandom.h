#pragma once
#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/types/smallmap.h"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"

namespace YBehavior
{
	class IDataRandomHelper
	{
	public:
		virtual ~IDataRandomHelper() {}
	public:
		virtual void Random(void* pLeft, const void* pRight0, const void* pRight1) const = 0;
		virtual void Random(IMemory* pMemory, IPin* pLeft, IPin* pRight0, IPin* pRight1) const = 0;
	};

	//struct NormalCalculateTag {};
	//struct InvalidCalculateTag {};
	//template<typename T> struct CalculateTagDispatchTrait {
	//	using Tag = NormalCalculateTag;
	//};
	//template<> struct CalculateTagDispatchTrait<EntityWrapper> {
	//	using Tag = InvalidCalculateTag;
	//};

	class ValueRandom
	{
	public:
		template<typename T>
		static void Random(void* pLeft, const void* pRight0, const void* pRight1)
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
	};

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class DataRandomHelper : public IDataRandomHelper
	{
		static DataRandomHelper<T> s_Instance;
	public:
		static IDataRandomHelper* Get() { return &s_Instance; }

		void Random(void* pLeft, const void* pRight0, const void* pRight1) const override
		{
			return ValueRandom::Random<T>(pLeft, pRight0, pRight1);
		}
		void Random(IMemory* pMemory, IPin* pLeft, IPin* pRight0, IPin* pRight1) const override
		{
			T left;
			ValueRandom::Random<T>(&left, pRight0->GetValuePtr(pMemory), pRight1->GetValuePtr(pMemory));
			pLeft->SetValue(pMemory, &left);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class DataRandomMgr : public Singleton<DataRandomMgr>
	{
		small_map<TYPEID, const IDataRandomHelper*> m_Randoms;
	public:
		DataRandomMgr();
		~DataRandomMgr();

		const IDataRandomHelper* Get(TYPEID t) const;

		template<typename T>
		const IDataRandomHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
	};
}
