#pragma once
#include "YBehavior/types.h"
#include "Ybehavior/agent.h"
#include "YBehavior/interface.h"

namespace YBehavior
{
	class IVectorHelper
	{
	public:
		virtual UINT GetVectorSize(const void* pVector) = 0;
		virtual void Clear(void* pVector) = 0;
		virtual void PushBack(void* pVector, void* value) = 0;
		virtual bool Set(void* pVector, int index, void* value) = 0;
		virtual const void* Get(void* pVector, int index) = 0;
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
				return ((const StdVector<T>*)pVector)->size();
			return 0;
		}
		void Clear(void* pVector) override
		{
			if (pVector)
				((StdVector<T>*)pVector)->clear();
		}

		void PushBack(void* pVector, void* value) override
		{
			if (pVector && value)
			{
				((StdVector<T>*)pVector)->emplace_back(*((const T*)value));
			}
		}
		bool Set(void* pVector, int index, void* value) override
		{
			if (pVector && value)
			{
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				if ((int)vec.size() <= index)
					return false;
				vec[index] = (*((const T*)value));
				return true;
			}
			return false;
		}
		const void* Get(void* pVector, int index) override
		{
			if (pVector)
			{
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				if ((int)vec.size() <= index)
					return nullptr;
				return &vec[index];
			}
			return nullptr;
		}
	};

	template<typename T>
	VectorHelper<T> VectorHelper<T>::s_Instance;

	template<>
	class VectorHelper<YBehavior::String> : public IVectorHelper
	{
	public:
		static VectorHelper<YBehavior::String> s_Instance;
	public:
		UINT GetVectorSize(const void* pVector) override
		{
			if (pVector)
				return ((const StdVector<YBehavior::String>*)pVector)->size();
			return 0;
		}
		void Clear(void* pVector) override
		{
			if (pVector)
				((StdVector<YBehavior::String>*)pVector)->clear();
		}

		///> CSTRING
		void PushBack(void* pVector, void* value) override
		{
			if (pVector && value)
			{
				((StdVector<YBehavior::String>*)pVector)->emplace_back((YBehavior::CSTRING)value);
			}
		}
		///> CSTRING
		bool Set(void* pVector, int index, void* value) override
		{
			if (pVector && value)
			{
				StdVector<YBehavior::String>& vec = *((StdVector<YBehavior::String>*)pVector);
				if ((int)vec.size() <= index)
					return false;
				vec[index] = (YBehavior::CSTRING)value;
				return true;
			}
			return false;
		}
		///> CSTRING
		const void* Get(void* pVector, int index) override
		{
			if (pVector)
			{
				StdVector<YBehavior::String>& vec = *((StdVector<YBehavior::String>*)pVector);
				if ((int)vec.size() <= index)
					return nullptr;
				return vec[index].c_str();
			}
			return nullptr;
		}
	};
	VectorHelper<YBehavior::String> VectorHelper<YBehavior::String>::s_Instance;

	template<>
	class VectorHelper<YBehavior::EntityWrapper> : public IVectorHelper
	{
	public:
		static VectorHelper<YBehavior::EntityWrapper> s_Instance;
	public:
		UINT GetVectorSize(const void* pVector) override
		{
			if (pVector)
				return ((const StdVector<YBehavior::EntityWrapper>*)pVector)->size();
			return 0;
		}
		void Clear(void* pVector) override
		{
			if (pVector)
				((StdVector<YBehavior::EntityWrapper>*)pVector)->clear();
		}

		///> Entity*
		void PushBack(void* pVector, void* value) override
		{
			if (pVector && value)
			{
				((StdVector<YBehavior::EntityWrapper>*)pVector)->emplace_back(((YBehavior::Entity*)value)->GetWrapper());
			}
		}
		///> Entity*
		bool Set(void* pVector, int index, void* value) override
		{
			if (pVector && value)
			{
				StdVector<YBehavior::EntityWrapper>& vec = *((StdVector<YBehavior::EntityWrapper>*)pVector);
				if ((int)vec.size() <= index)
					return false;
				vec[index] = ((Entity*)value)->GetWrapper();
				return true;
			}
			return false;
		}
		const void* Get(void* pVector, int index) override
		{
			if (pVector)
			{
				StdVector<YBehavior::EntityWrapper>& vec = *((StdVector<YBehavior::EntityWrapper>*)pVector);
				if ((int)vec.size() <= index)
					return nullptr;
				if (vec[index].IsValid())
					return vec[index].Get();
			}
			return nullptr;
		}
	};

	VectorHelper<YBehavior::EntityWrapper> VectorHelper<YBehavior::EntityWrapper>::s_Instance;

	class VectorHelperMgr
	{
	protected:
		static IVectorHelper* _Helpers[8];
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

			_Helpers[0] = nullptr;
		}
		static VectorHelperMgr s_Mgr;
	};

	YBehavior::IVectorHelper* VectorHelperMgr::_Helpers[8];
	YBehavior::VectorHelperMgr VectorHelperMgr::s_Mgr;
}

extern "C" YBEHAVIOR_API YBehavior::UINT VectorGetSize(const void* pVector, YBehavior::TYPEID type)
{
	return YBehavior::VectorHelperMgr::Get(type)->GetVectorSize(pVector);
}

extern "C" YBEHAVIOR_API void VectorClear(void* pVector, YBehavior::TYPEID type)
{
	YBehavior::VectorHelperMgr::Get(type)->Clear(pVector);
}
extern "C" YBEHAVIOR_API void VectorPushBack(void* pVector, void* value, YBehavior::TYPEID type)
{
	YBehavior::VectorHelperMgr::Get(type)->PushBack(pVector, value);
}
extern "C" YBEHAVIOR_API bool VectorSet(void* pVector, int index, void* value, YBehavior::TYPEID type)
{
	return YBehavior::VectorHelperMgr::Get(type)->Set(pVector, index, value);
}
extern "C" YBEHAVIOR_API const void* VectorGet(void* pVector, int index, YBehavior::TYPEID type)
{
	return YBehavior::VectorHelperMgr::Get(type)->Get(pVector, index);
}
