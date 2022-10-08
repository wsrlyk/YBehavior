#ifndef _YBEHAVIOR_VARIABLEOPERATION_H_
#define _YBEHAVIOR_VARIABLEOPERATION_H_

#define YB_ABOUT_EQUAL_FLOAT

#include "YBehavior/interface.h"
#include "YBehavior/tools/bimap.h"
#include "YBehavior/types.h"
#include "YBehavior/logger.h"
#include "YBehavior/tools/objectpool.h"
#include <map>
namespace YBehavior
{
	class IVariableOperationHelper;
	struct TempObject
	{
		void* pData{ nullptr };
		const IVariableOperationHelper* pHelper{ nullptr };

		TempObject(void* data, const IVariableOperationHelper* helper)
			: pData(data), pHelper(helper)
		{}
		~TempObject();

	private:
		TempObject(const TempObject& o) = delete;
		// copy elision is required.
		TempObject(TempObject&& o) = delete;	
		TempObject& operator=(const TempObject& o) = delete;
	};

	class IVariableOperationHelper
	{
	public:
		virtual ~IVariableOperationHelper() {}
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
	class VariableOperationHelper : public IVariableOperationHelper
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
	class VariableOperationMgr : public Singleton<VariableOperationMgr>
	{
		std::map<TYPEID, const IVariableOperationHelper*> m_Operations;
	public:
		VariableOperationMgr();
		~VariableOperationMgr();

		const IVariableOperationHelper* Get(TYPEID t) const;

		template<typename T>
		const IVariableOperationHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
	};

}

#endif
