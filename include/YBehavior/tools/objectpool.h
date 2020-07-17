#ifndef _YBEHAVIOR_OBJECTPOOL_H_
#define _YBEHAVIOR_OBJECTPOOL_H_
#include "YBehavior/types.h"
#include <list>
#include "YBehavior/utility.h"

namespace YBehavior
{
	template <typename T>
	class ObjectPool
	{
		static std::list<T*> s_Pool;

	public:
		static T* Get()
		{
			if (s_Pool.empty())
				return new T();
			T* t = s_Pool.front();
			s_Pool.pop_front();
			Utility::SetDefault<T>(*t);
			return t;
		}

		static void Recycle(T* t);
	};

	template <typename T>
	std::list<T*> ObjectPool<T>::s_Pool;

	template <typename T>
	void YBehavior::ObjectPool<T>::Recycle(T* t)
	{
		if (t == nullptr)
			return;
		s_Pool.push_back(t);
	}

	template<> 
	void YBehavior::ObjectPool<EntityWrapper>::Recycle(EntityWrapper* t);

}

#endif