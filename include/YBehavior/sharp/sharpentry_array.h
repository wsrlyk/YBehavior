#ifdef YSHARP
#pragma once
#include "YBehavior/types/types.h"
#include "YBehavior/agent.h"
#include "YBehavior/interface.h"
#include "sharpentry_buffer.h"

namespace YBehavior
{
	class IArrayHelper
	{
	public:
		virtual UINT GetSize(const void* pVector) = 0;
		virtual void Clear(void* pVector) = 0;
		virtual void PushBack(void* pVector) = 0;
		virtual bool Set(void* pVector, int index) = 0;
		virtual bool Get(void* pVector, int index) = 0;
		virtual bool Erase(void* pVector) = 0;
		virtual bool EraseAt(void* pVector, int index) = 0;
		virtual int Find(void* pVector) = 0;
		virtual bool Assign(void* pFrom, void* pTo) = 0;
	};

	template<typename T>
	class ArrayHelper : public IArrayHelper
	{
	public:
		static ArrayHelper<T> s_Instance;
	public:
		UINT GetSize(const void* pVector) override
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
		bool Erase(void* pVector) override
		{
			if (pVector)
			{
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				T value = *SharpBuffer::Get<T>();
				auto it = std::find(vec.begin(), vec.end(), value);
				if (it != vec.end())
				{
					vec.erase(it);
					return true;
				}

				return false;
			}
			return false;
		}
		bool EraseAt(void* pVector, int index) override
		{
			if (pVector)
			{
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				if (index >= 0 && index < (int)vec.size())
				{
					vec.erase(vec.begin() + index);
					return true;
				}

				return false;
			}
			return false;
		}
		int Find(void* pVector) override
		{
			if (pVector)
			{
				StdVector<T>& vec = *((StdVector<T>*)pVector);
				auto it = std::find(vec.begin(), vec.end(), *SharpBuffer::Get<T>());
				if (it != vec.end())
					return (int)(it - vec.begin());
				return -1;
			}
			return -1;
		}
		bool Assign(void* pFrom, void* pTo) override
		{
			if (pFrom && pTo)
			{
				StdVector<T>& from = *((StdVector<T>*)pFrom);
				StdVector<T>& to = *((StdVector<T>*)pTo);
				to.assign(from.begin(), from.end());
				return true;
			}
			return false;
		}

	};

	template<typename T>
	ArrayHelper<T> ArrayHelper<T>::s_Instance;

	class ArrayHelperMgr
	{
	protected:
		static IArrayHelper* _Helpers[7];
	public:

		static IArrayHelper* Get(const TYPEID& k)
		{
			return _Helpers[k];
		}
	private:
		template<typename T>
		static void Register(const TYPEID& k)
		{
			_Helpers[k] = &ArrayHelper<T>::s_Instance;
		}

		ArrayHelperMgr()
		{
#define REGISTER_ArrayHelper(T)\
	ArrayHelperMgr::Register<T>(GetTypeID<T>())

			REGISTER_ArrayHelper(Int);
			REGISTER_ArrayHelper(Ulong);
			REGISTER_ArrayHelper(Bool);
			REGISTER_ArrayHelper(Float);
			REGISTER_ArrayHelper(String);
			REGISTER_ArrayHelper(EntityWrapper);
			REGISTER_ArrayHelper(Vector3);
		}
		static ArrayHelperMgr s_Mgr;
	};

	YBehavior::IArrayHelper* ArrayHelperMgr::_Helpers[7];
	YBehavior::ArrayHelperMgr ArrayHelperMgr::s_Mgr;
}

extern "C" YBEHAVIOR_API YBehavior::UINT ArrayGetSize(void* pVector, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->GetSize(pVector);
}
extern "C" YBEHAVIOR_API void ArrayClear(void* pVector, YBehavior::TYPEID type)
{
	YBehavior::ArrayHelperMgr::Get(type)->Clear(pVector);
}
extern "C" YBEHAVIOR_API void ArrayPushBack(void* pVector, YBehavior::TYPEID type)
{
	YBehavior::ArrayHelperMgr::Get(type)->PushBack(pVector);
}
extern "C" YBEHAVIOR_API bool ArraySet(void* pVector, int index, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->Set(pVector, index);
}
extern "C" YBEHAVIOR_API bool ArrayGet(void* pVector, int index, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->Get(pVector, index);
}
extern "C" YBEHAVIOR_API bool ArrayErase(void* pVector, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->Erase(pVector);
}
extern "C" YBEHAVIOR_API bool ArrayEraseAt(void* pVector, int index, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->EraseAt(pVector, index);
}
extern "C" YBEHAVIOR_API int ArrayFind(void* pVector, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->Find(pVector);
}
extern "C" YBEHAVIOR_API bool ArrayAssign(void* pFrom, void* pTo, YBehavior::TYPEID type)
{
	return YBehavior::ArrayHelperMgr::Get(type)->Assign(pFrom, pTo);
}
extern "C" YBEHAVIOR_API int ArrayGetEntityIndex(void* pVector, int index)
{
	if (pVector)
	{
		StdVector<YBehavior::EntityWrapper>& vec = *((StdVector<YBehavior::EntityWrapper>*)pVector);
		if ((int)vec.size() <= index || index < 0)
			return -1;

		auto& wrapper = vec[index];
		if (!wrapper.IsValid())
			return -1;
		if (auto e = static_cast<YBehavior::SharpEntity*>(wrapper.Get()))
		{
			return e->GetIndex();
		}
	}
	return -1;
}

#endif