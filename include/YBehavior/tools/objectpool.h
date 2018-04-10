#ifndef _YBEHAVIOR_BIMAP_H_
#define _YBEHAVIOR_BIMAP_H_

#include <list>

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
			return t;
		}

		static void Recycle(T* t)
		{
			if (t == nullptr)
				return;
			s_Pool.push_back(t);
		}
	};

	template <typename T>
	std::list<T*> ObjectPool<T>::s_Pool;
}

#endif