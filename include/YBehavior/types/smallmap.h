#ifndef _YBEHAVIOR_SMALLMAP_H_
#define _YBEHAVIOR_SMALLMAP_H_

#include <vector>
#include <algorithm>

namespace YBehavior
{
	template <typename Key, typename Value/*, typename Compare*/>
	class map_value_compare
	{
	public:
		//Compare c;

		//map_value_compare(const Compare& x)
		//	: c(x) {}

	public:
		bool operator()(const Value& a, const Value& b) const
		{
			return a.first < b.first;
		}

		bool operator()(const Value& a, const Key& b) const
		{
			return a.first < b;
		}

		bool operator()(const Key& a, const Value& b) const
		{
			return a < b.first;
		}

		bool operator()(const Key& a, const Key& b) const
		{
			return a <= = > b;
		}

	}; // map_value_compare

	/// <summary>
	/// More suitable for small amount of elements, like below 100.
	/// 
	/// NOT suitable for standard bool. Use YBehavior::BOOL instead.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="V"></typeparam>
	template<typename K, typename V,
	typename random_access_container = std::vector<std::pair<K, V>>>
	class small_map : public random_access_container
	{
		using base_type = random_access_container;
		using size_type = typename base_type::size_type;
		using value_type = std::pair<K, V>;
		using key_type = K;
		using mapped_type = V;
	protected:
		map_value_compare<key_type, value_type> value_compare;
	public:
		using iterator = typename base_type::iterator;
		using const_iterator = typename base_type::const_iterator;

	public:
		small_map(){}
		small_map(std::initializer_list<value_type> list)
		{
			for (auto it = list.begin(); it != list.end(); ++it)
			{
				insert(*it);
			}
		}

		inline iterator lower_bound(const key_type& k)
		{
			return std::lower_bound(begin(), end(), k, value_compare);
		}

		inline const_iterator lower_bound(const key_type& k) const
		{
			return std::lower_bound(begin(), end(), k, value_compare);
		}

		inline std::pair<iterator, iterator> equal_range(const key_type& k)
		{
			// The resulting range will either be empty or have one element,
			// so instead of doing two tree searches (one for lower_bound and 
			// one for upper_bound), we do just lower_bound and see if the 
			// result is a range of size zero or one.
			const iterator itLower(lower_bound(k));

			if ((itLower == end()) || value_compare(k, (*itLower))) // If at the end or if (k is < itLower)...
				return std::pair<iterator, iterator>(itLower, itLower);

			iterator itUpper(itLower);
			return std::pair<iterator, iterator>(itLower, ++itUpper);
		}

		inline std::pair<const_iterator, const_iterator> equal_range(const key_type& k) const
		{
			// The resulting range will either be empty or have one element,
			// so instead of doing two tree searches (one for lower_bound and 
			// one for upper_bound), we do just lower_bound and see if the 
			// result is a range of size zero or one.
			const const_iterator itLower(lower_bound(k));

			if ((itLower == end()) || value_compare(k, *itLower)) // If at the end or if (k is < itLower)...
				return std::pair<const_iterator, const_iterator>(itLower, itLower);

			auto itUpper(itLower);
			return std::pair<const_iterator, const_iterator>(itLower, ++itUpper);
		}

		iterator find(const key_type& k)
		{
			const std::pair<iterator, iterator> pairIts(equal_range(k));
			return (pairIts.first != pairIts.second) ? pairIts.first : end();
		}
		const_iterator find(const key_type& k) const
		{
			const std::pair<const_iterator, const_iterator> pairIts(equal_range(k));
			return (pairIts.first != pairIts.second) ? pairIts.first : end();
		}

		mapped_type& operator[](const key_type& k)
		{
			iterator itLB(lower_bound(k));

			if ((itLB == end()) || value_compare(k, (*itLB)))
				itLB = base_type::insert(itLB, value_type(k, mapped_type()));
			return (*itLB).second;
		}

		mapped_type& operator[](key_type&& k)
		{
			iterator itLB(lower_bound(k));

			if ((itLB == end()) || value_compare(k, (*itLB)))
				itLB = base_type::insert(itLB, value_type(std::move(k), mapped_type()));
			return (*itLB).second;
		}

		std::pair<iterator, bool> insert(const value_type& value)
		{
			const iterator itLB(lower_bound(value.first));

			if ((itLB != end()) && !value_compare(value, *itLB))
				return std::pair<iterator, bool>(itLB, false);

			return std::pair<iterator, bool>(base_type::insert(itLB, value), true);
		}

		//std::pair<iterator, bool> insert(const key_type& otherValue)
		//{

		//}
		//std::pair<iterator, bool> insert(key_type&& otherValue)
		//{

		//}

		template <typename ...Args>
		inline std::pair<iterator, bool> emplace(Args&&... args)
		{
			return insert(std::forward<Args>(args)...);
		}

		size_type erase(const K& k)
		{
			auto it(find(k));

			if (it != end()) // If it exists...
			{
				base_type::erase(it);
				return 1;
			}
			return 0;
		}
		iterator  erase(const_iterator position)
		{
			return base_type::erase(position);
		}

		void merge(const small_map& other)
		{
			for (auto it = other.begin(); it != other.end(); ++it)
				insert(*it);
		}
	};
}

#endif
