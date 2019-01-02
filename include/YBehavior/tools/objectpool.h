#ifndef _YBEHAVIOR_OBJECTPOOL_H_
#define _YBEHAVIOR_OBJECTPOOL_H_
#include "YBehavior/types.h"
#include <list>
#include "YBehavior/utility.h"
#include <unordered_set>

namespace YBehavior
{
	class ObjectPoolHelper
	{
	public:
		static std::unordered_set<void*> s_UnRecycled;
		//static std::unordered_set<void*> s_Deleted;
	};
	template <typename T>
	class ObjectPool
	{
		static std::list<T*> s_Pool;
	public:
		static T* Get()
		{
			T* t = nullptr;
			if (s_Pool.empty())
			{
				t = new T();
				ObjectPoolHelper::s_UnRecycled.insert(t);
				return t;
			}
			t = s_Pool.front();
			s_Pool.pop_front();
			Utility::SetDefault<T>(*t);
			auto it = ObjectPoolHelper::s_UnRecycled.find(t);
			if (it != ObjectPoolHelper::s_UnRecycled.end())
			{
				ERROR_BEGIN << "it's already used: " << t << ERROR_END;
			}
			else
			{
				ObjectPoolHelper::s_UnRecycled.insert(t);
			}
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

		auto it = ObjectPoolHelper::s_UnRecycled.find(t);
		if (it == ObjectPoolHelper::s_UnRecycled.end())
		{
			ERROR_BEGIN << "it's already recycled: " << t << ERROR_END;
		}
		else
		{
			ObjectPoolHelper::s_UnRecycled.erase(it);
		}
	}

	template<> 
	void YBehavior::ObjectPool<EntityWrapper>::Recycle(EntityWrapper* t);

}

#endif