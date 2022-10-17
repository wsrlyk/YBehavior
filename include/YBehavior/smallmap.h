#ifndef _YBEHAVIOR_SMALLMAP_H_
#define _YBEHAVIOR_SMALLMAP_H_

#include <vector>

namespace YBehavior
{
	/// <summary>
	/// More suitable for small amount of elements, like below 100.
	/// 
	/// NOT suitable for standard bool. Use YBehavior::BOOL instead.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="V"></typeparam>
	template<typename K, typename V>
	class small_map
	{
		template<typename T>
		using container_type = std::vector<T>;
		using size_type = typename container_type<K>::size_type;

	public:
		class const_iterator;
		class iterator;
		class pair
		{
			friend const_iterator;
			friend iterator;
			small_map* _Map;
			size_type _Idx;
		public:
			pair(small_map* map, size_type idx)
				: _Map(map)
				, _Idx(idx)
			{}
			bool operator==(const pair& other) const
			{
				return _Idx == other._Idx && _Map == other._Map;
			}
			bool operator!=(const pair& other) const
			{
				return _Idx != other._Idx || _Map != other._Map;
			}

			const K& first() const { return _Map->keys()[_Idx]; }
			const V& second() const { return _Map->values()[_Idx]; }
			size_type idx() const { return _Idx; }
			V& second() { return _Map->values()[_Idx]; }
		};
		class const_iterator
		{
		public:
			const_iterator()
				: _Pair(nullptr, 0)
				, _Size(0)
			{}
			const_iterator(const small_map* pMap)
				: _Pair(const_cast<small_map*>(pMap), 0)
			{
				_Size = pMap->size();
			}
			const_iterator(size_type cur, const small_map* pMap)
				: _Pair(const_cast<small_map*>(pMap), cur)
			{
				_Size = pMap->size();
			}

			const_iterator& operator++()
			{
				++_Pair._Idx;
				return *this;
			}
			const_iterator operator++(int)
			{	// postincrement
				const_iterator tmp = *this;
				++* this;
				return (tmp);
			}

			bool operator==(const const_iterator& other) const
			{
				return _Pair == other._Pair;
			}
			bool operator!=(const const_iterator& other) const
			{
				return _Pair != other._Pair;
			}

			const pair& operator*() const
			{
				return _Pair;
			}

			const pair* operator->() const
			{
				return &_Pair;
			}


		protected:
			pair _Pair;
			size_type _Size;
		};

		class iterator : public const_iterator
		{
			using base = const_iterator;
		public:
			iterator(small_map* pMap)
				: const_iterator(pMap)
			{}
			iterator(size_type cur, small_map* pMap)
				: const_iterator(cur, pMap)
			{}

			iterator& operator++()
			{
				base::operator++();
				return *this;
			}
			iterator operator++(int)
			{	// postincrement
				iterator tmp = *this;
				base::operator++();
				return tmp;
			}

			pair& operator*()
			{
				return this->_Pair;
			}

			pair* operator->()
			{
				return &this->_Pair;
			}
		};

	protected:
		container_type<K> _Keys;
		container_type<V> _Values;

	public:
		small_map(){}
		small_map(std::initializer_list<std::pair<K, V>> list)
		{
			for (auto it = list.begin(); it != list.end(); ++it)
			{
				insert(it->first, it->second);
			}
		}
		const container_type<K>& keys() const { return _Keys; }
		const container_type<V>& values() const { return _Values; }
		container_type<V>& values() { return _Values; }

		size_type size() const { return _Keys.size(); }

		iterator find(const K& k) {
			return iterator(_find(k), this);
		}

		const_iterator find(const K& k) const {
			return const_iterator(_find(k), this);
		}

		iterator begin() {
			return iterator(0, this);
		}

		const_iterator begin() const {
			return const_iterator(0, this);
		}

		iterator end() {
			return iterator(size(), this);
		}

		const_iterator end() const {
			return const_iterator(size(), this);
		}

		void clear()
		{
			_Keys.clear();
			_Values.clear();
		}
		template<typename KK>
		V& operator[](KK&& k)
		{
			size_type cur = 0;
			if (_find(k, cur))
			{
				return _Values[cur];
			}
			_Keys.emplace(_Keys.begin() + cur, k);
			_Values.emplace(_Values.begin() + cur);
			return _Values[cur];
		}

		template<typename KK, typename VV>
		std::pair<pair, bool> insert(KK&& k, VV&& v)
		{
			size_type cur = 0;
			if (_find(k, cur))
			{
				return std::pair<pair, bool>(pair(this, cur), false);
			}
			_Keys.emplace(_Keys.begin() + cur, k);
			_Values.emplace(_Values.begin() + cur, v);
			return std::pair<pair, bool>(pair(this, cur), true);
		}

		iterator erase(iterator it)
		{
			if (it->idx() < size())
			{
				_Keys.erase(_Keys.begin() + it->idx());
				_Values.erase(_Values.begin() + it->idx());
				return iterator(it->idx(), this);
			}
			else
			{
				return it;
			}
		}

		size_type erase(const K& k)
		{
			auto it = find(k);
			if (it == end())
				return 0;
			erase(it);
			return 1;
		}


	protected:
		bool _find(const K& k, size_type& cur) const
		{
			auto res = std::lower_bound(_Keys.begin(), _Keys.end(), k);
			cur = res - _Keys.begin();
			return res != _Keys.end() && * res == k;


			//int len = (int)_Keys.size();
			//int start = 0;
			//int end = len - 1;
			//while (start <= end)
			//{
			//	auto mid = (start + end) >> 1;
			//	if (k < _Keys[mid])
			//		end = mid - 1;
			//	else if (k > _Keys[mid])
			//		start = mid + 1;
			//	else
			//	{
			//		cur = (size_type)mid;
			//		return true;
			//	}
			//}
			//cur = (size_type)len;
			//return false;
		}

		size_type _find(const K& k) const
		{
			size_type cur = 0;
			if (_find(k, cur))
				return cur;
			return _Keys.size();
		}
	};
}

#endif
