#ifdef YSHARP
#pragma once
#include "YBehavior/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"
#include "sharpentry_buffer.h"

namespace YBehavior
{
	class IVectorHelper
	{
	public:
		virtual UINT GetVectorSize(const void* pVector) = 0;
		virtual void Clear(void* pVector) = 0;
		virtual void PushBack(void* pVector) = 0;
		virtual bool Set(void* pVector, int index) = 0;
		virtual bool Get(void* pVector, int index) = 0;
		///> void SetIntVectorAtIndex(VecInt* pVec, int index, int value)
		///> int GetIntVectorAtIndex(VecInt* pVec, int index)
	};

	template<typename T>
	class VectorHelper : public IVectorHelper
	{
	public:
		static VectorHelper<T> s_Instance;
	public:
		UINT GetVectorSize(const void* pVector) override
		{
			if (pVector)
				return (UINT)((const StdVector<T>*)pVector)->size();
			return 0;
		}
		void Clear(void* pVector) override
		{
			if (pVector)
				((StdVector<T>*)pVector)->clear();
		}

		void PushBack(void* pVector) override
		{
			if (pVector)
			{
				T* value = SharpBuffer::Get<T>();
				((StdVector<T>*)pVector)->emplace_back(*value);
			}
		}
		bool Set(void* pVector, int index) override
		{
			if (pVector)
			{
				T* value = SharpBuffer::Get<T>();
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				if ((int)vec.size() <= index)
					return false;
				vec[index] = *value;
				return true;
			}
			return false;
		}
		bool Get(void* pVector, int index) override
		{
			if (pVector)
			{
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				if ((int)vec.size() <= index)
					return false;

				T* value = SharpBuffer::Get<T>();
				*value = vec[index];

				return true;
			}
			return false;
		}
	};

	template<typename T>
	VectorHelper<T> VectorHelper<T>::s_Instance;

	class VectorHelperMgr
	{
	protected:
		static IVectorHelper* _Helpers[7];
	public:

		static IVectorHelper* Get(const TYPEID& k)
		{
			return _Helpers[k];
		}
	private:
		template<typename T>
		static void Register(const TYPEID& k)
		{
			_Helpers[k] = &VectorHelper<T>::s_Instance;
		}

		VectorHelperMgr()
		{
#define REGISTER_VectorHelper(T)\
	VectorHelperMgr::Register<T>(GetTypeID<T>())

			REGISTER_VectorHelper(Int);
			REGISTER_VectorHelper(Ulong);
			REGISTER_VectorHelper(Bool);
			REGISTER_VectorHelper(Float);
			REGISTER_VectorHelper(String);
			REGISTER_VectorHelper(EntityWrapper);
			REGISTER_VectorHelper(Vector3);
		}
		static VectorHelperMgr s_Mgr;
	};

	YBehavior::IVectorHelper* VectorHelperMgr::_Helpers[7];
	YBehavior::VectorHelperMgr VectorHelperMgr::s_Mgr;
}

extern "C" YBEHAVIOR_API YBehavior::UINT VectorGetSize(void* pVector, YBehavior::TYPEID type)
{
	return YBehavior::VectorHelperMgr::Get(type)->GetVectorSize(pVector);
}

extern "C" YBEHAVIOR_API void VectorClear(void* pVector, YBehavior::TYPEID type)
{
	YBehavior::VectorHelperMgr::Get(type)->Clear(pVector);
}
extern "C" YBEHAVIOR_API void VectorPushBack(void* pVector, YBehavior::TYPEID type)
{
	YBehavior::VectorHelperMgr::Get(type)->PushBack(pVector);
}
extern "C" YBEHAVIOR_API bool VectorSet(void* pVector, int index, YBehavior::TYPEID type)
{
	return YBehavior::VectorHelperMgr::Get(type)->Set(pVector, index);
}
extern "C" YBEHAVIOR_API bool VectorGet(void* pVector, int index, YBehavior::TYPEID type)
{
	return YBehavior::VectorHelperMgr::Get(type)->Get(pVector, index);
}
#endif