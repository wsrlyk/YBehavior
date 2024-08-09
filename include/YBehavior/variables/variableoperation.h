#ifndef _YBEHAVIOR_VARIABLEOPERATION_H_
#define _YBEHAVIOR_VARIABLEOPERATION_H_

#define YB_ABOUT_EQUAL_FLOAT

#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/tools/objectpool.h"
#include "YBehavior/types/smallmap.h"
#include "YBehavior/singleton.h"
namespace YBehavior
{
	class IDataOperationHelper;
	struct TempObject
	{
		void* pData{ nullptr };
		const IDataOperationHelper* pHelper{ nullptr };

		TempObject(void* data, const IDataOperationHelper* helper)
			: pData(data), pHelper(helper)
		{}
		~TempObject();

	private:
		TempObject(const TempObject& o) = delete;
		// copy elision is required.
		TempObject(TempObject&& o) = delete;	
		TempObject& operator=(const TempObject& o) = delete;
	};

	class IDataOperationHelper
	{
	public:
		virtual ~IDataOperationHelper() {}
		virtual void Set(void* pLeft, const void* pRight0) const = 0;
		virtual void* AllocData() const = 0;
		virtual TempObject AllocTempData() const = 0;
		virtual void RecycleData(void* pData) const = 0;
	};

	class ValueOperation
	{
	public:
		template<typename T>
		static void Set(void* pLeft, const void* pRight0)
		{
			T& left = *((T*)pLeft);
			left = *((T*)pRight0);
		}
	};

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class DataOperationHelper : public IDataOperationHelper
	{
	public:
		void Set(void* pLeft, const void* pRight0) const override
		{
			ValueOperation::Set<T>(pLeft, pRight0);
		}

		TempObject AllocTempData() const override
		{
			return TempObject(ObjectPoolStatic<T>::Get(), this);
		}

		void* AllocData() const override
		{
			return ObjectPoolStatic<T>::Get();
		}

		void RecycleData(void* pData) const override
		{
			if (pData != nullptr)
				ObjectPoolStatic<T>::Recycle((T*)pData);
		}

	};

	/////////////////////////////////////
/////////////////////////////////////
/////////////////////////////////////
	class DataOperationMgr : public Singleton<DataOperationMgr>
	{
		small_map<TYPEID, const IDataOperationHelper*> m_Operations;
	public:
		DataOperationMgr();
		~DataOperationMgr();

		const IDataOperationHelper* Get(TYPEID t) const;

		template<typename T>
		const IDataOperationHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
	};

}

#endif
