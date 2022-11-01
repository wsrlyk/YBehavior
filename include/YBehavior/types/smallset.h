#ifndef _YBEHAVIOR_SMALLSET_H_
#define _YBEHAVIOR_SMALLSET_H_

#include <vector>
#include <algorithm>

namespace YBehavior
{
	/// <summary>
	/// More suitable for small amount of elements, like below 100.
	/// 
	/// NOT suitable for standard bool. Use YBehavior::BOOL instead.
	/// </summary>
	/// <typeparam name="K"></typeparam>
	/// <typeparam name="V"></typeparam>
	template<typename T>
	class small_set
	{
		template<typename T>
		using container_type = std::vector<T>;
		using size_type = typename container_type<T>::size_type;

	public:
		class const_iterator;
		class iterator;
		class const_iterator
		{
		public:
			const_iterator()
				: _Set(nullptr)
				, _Idx(0)
				, _Size(0)
			{}
			const_iterator(const small_set* pSet)
				: _Set(pSet)
				, _Idx(0)
				, _Size(pSet->size())
			{
			}
			const_iterator(const small_set* pSet, size_type cur)
				: _Set(pSet)
				, _Idx(cur)
				, _Size(pSet->size())
			{
			}

			const_iterator& operator++()
			{
				++_Idx;
				return *this;
			}
			const_iterator operator++(int)
			{
				const_iterator tmp = *this;
				++* this;
				return (tmp);
			}

			bool operator==(const const_iterator& other) const
			{
				return _Set == other._Set && _Idx == other._Idx;
			}
			bool operator!=(const const_iterator& other) const
			{
				return _Set != other._Set || _Idx != other._Idx;
			}

			const T& operator*() const
			{
				return _Set->_Values[_Idx];
			}

			const T* operator->() const
			{
				return &_Set->_Values[_Idx];
			}

			inline size_type idx() const { return _Idx; }
		protected:
			const small_set* _Set;
			size_type _Idx;
			size_type _Size;
		};

		class iterator : public const_iterator
		{
			using base = const_iterator;
		public:
			iterator(small_set* pSet)
				: const_iterator(pSet)
			{}
			iterator(small_set* pSet, size_type cur)
				: const_iterator(pSet, cur)
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

			T& operator*()
			{
				return _Set->_Values[_Idx];
			}

			T* operator->()
			{
				return &_Set->_Values[_Idx];
			}
		};

	protected:
		container_type<T> _Values;

	public:
		small_set(){}
		small_set(std::initializer_list<T> list)
		{
			for (auto it = list.begin(); it != list.end(); ++it)
			{
				insert(*it);
			}
		}

		size_type size() const { return _Values.size(); }

		iterator find(const T& t)
		{
			return iterator(this, _find(t));
		}

		const_iterator find(const T& t) const 
		{
			return const_iterator(this, _find(t));
		}

		bool contains(const T& t) const 
		{
			size_type cur;
			return _find(t, cur);
		}

		iterator begin() 
		{
			return iterator(this, 0);
		}

		const_iterator begin() const 
		{
			return const_iterator(this, 0);
		}

		iterator end() 
		{
			return iterator(this, size());
		}

		const_iterator end() const 
		{
			return const_iterator(this, size());
		}

		void clear()
		{
			_Values.clear();
		}

		template<typename TT>
		std::pair<iterator, bool> insert(TT&& t)
		{
			size_type cur = 0;
			if (_find(t, cur))
			{
				return std::pair<iterator, bool>(iterator(this, cur), false);
			}
			_Values.emplace(_Values.begin() + cur, t);
			return std::pair<iterator, bool>(iterator(this, cur), true);
		}

		iterator erase(iterator it)
		{
			if (it->idx() < size())
			{
				_Values.erase(_Values.begin() + it->idx());
				return iterator(this, it->idx());
			}
			else
			{
				return it;
			}
		}

		size_type erase(const T& t)
		{
			if (_find(t, auto cur))
			{
				erase(iterator(this, cur));
				return 1;
			}
			return 0;
		}

		void merge(const small_set& other)
		{
			for (const auto& v : other._Values)
			{
				size_type cur;
				if (!_find(v, cur))
				{
					_Values.emplace(_Values.begin() + cur, v);
				}
			}
		}
	protected:
		bool _find(const T& t, size_type& cur) const
		{
			auto res = std::lower_bound(_Values.begin(), _Values.end(), t);
			cur = res - _Values.begin();
			return res != _Values.end() && *res == t;


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

		size_type _find(const T& t) const
		{
			size_type cur = 0;
			if (_find(t, cur))
				return cur;
			return _Values.size();
		}
	};
}

#endif
