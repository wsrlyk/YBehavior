#ifndef _YBEHAVIOR_OBJECTPOOL_H_
#define _YBEHAVIOR_OBJECTPOOL_H_
#include "YBehavior/types/types.h"
#include <vector>
#include "YBehavior/utility.h"

namespace YBehavior
{
	template <typename T>
	class ObjectPool
	{
		std::vector<T*> m_Pool;

	public:
		~ObjectPool();
		T* Fetch();
		T* FetchNotReset();

		void Return(T* t);
	};

	template <typename T>
	ObjectPool<T>::~ObjectPool()
	{
		for (auto o : m_Pool)
		{
			delete o;
		}
		m_Pool.clear();
	}

	template <typename T>
	T* ObjectPool<T>::Fetch()
	{
		if (m_Pool.empty())
			return new T();
		T* t = m_Pool.back();
		m_Pool.pop_back();
		Utility::SetDefault(*t);
		return t;
	}

	template <typename T>
	T* ObjectPool<T>::FetchNotReset()
	{
		if (m_Pool.empty())
			return new T();
		T* t = m_Pool.back();
		m_Pool.pop_back();
		return t;
	}

	template <typename T>
	void ObjectPool<T>::Return(T* t)
	{
		if (t == nullptr)
			return;
		m_Pool.push_back(t);
	}

	template<> 
	void ObjectPool<EntityWrapper>::Return(EntityWrapper* t);

	//////////////////////////////////////////////////////////////////////////

	template <typename T>
	class ObjectPoolStatic : public ObjectPool<T>
	{
		static ObjectPoolStatic s_Pool;
	public:
		static T* Get() { return s_Pool.Fetch(); }
		static void Recycle(T* t) { s_Pool.Return(t); }
	};

	template <typename T>
	ObjectPoolStatic<T> ObjectPoolStatic<T>::s_Pool;

}

#endif