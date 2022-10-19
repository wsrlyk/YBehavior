#ifndef _YBEHAVIOR_BIMAP_H_
#define _YBEHAVIOR_BIMAP_H_

#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	// without this hasher, it will fail when key is enum
	struct EnumClassHash
	{
		template <typename T>
		std::size_t operator()(T t) const
		{
			return static_cast<std::size_t>(t);
		}
	};

	template<typename T0, typename T1>
	class Bimap
	{
		small_map<T0, T1> map0;
		small_map<T1, T0> map1;

	public:
		typedef std::pair<T0, T1> Pair;
		Bimap(const std::initializer_list<Pair>& list)
		{
			for (auto it = list.begin(); it != list.end(); ++it)
			{
				map0[it->first] = it->second;
				map1[it->second] = it->first;
			}
		}
		void Clear()
		{
			map0.clear();
			map1.clear();
		}
		void Set(const T0& t0, const T1& t1)
		{
			RemoveKey(t0);
			RemoveValue(t1);
			map0[t0] = t1;
			map1[t1] = t0;
		}
		bool RemoveKey(const T0& t0)
		{
			T1 t1;
			if (TryGetValue(t0, t1))
			{
				map0.erase(t0);
				map1.erase(t1);
				return true;
			}
			return false;
		}
		bool RemoveValue(const T1& t1)
		{
			T0 t0;
			if (TryGetKey(t1, t0))
			{
				map0.erase(t0);
				map1.erase(t1);
				return true;
			}
			return false;
		}
		
		bool TryGetValue(const T0& t0, T1& t1) const
		{
			auto it = map0.find(t0);
			if (it == map0.end())
				return false;

			t1 = it->second();
			return true;
		}
		bool TryGetKey(const T1& t1, T0& t0) const
		{
			auto it = map1.find(t1);
			if (it == map1.end())
				return false;

			t0 = it->second();
			return true;
		}

		const T1& GetValue(const T0& t0, const T1& defaultRes) const
		{
			auto it = map0.find(t0);
			if (it == map0.end())
				return defaultRes;
			return it->second();
		}

		const T0& GetKey(const T1& t1, const T0& defaultRes) const
		{
			auto it = map1.find(t1);
			if (it == map1.end())
				return defaultRes;
			return it->second();
		}

	};
}

#endif