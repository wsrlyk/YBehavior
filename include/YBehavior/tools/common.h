#ifndef _YBEHAVIOR_COMMON_H_
#define _YBEHAVIOR_COMMON_H_

#include "YBehavior/types.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include <set>

namespace YBehavior
{
	class RandomIndex
	{
	public:
		void Rand();
		void Append() { m_IndexList.push_back((int)m_IndexList.size()); }
		void Set(int length);
		void Clear();
		int operator[] (int index);
		const StdVector<int>& GetIndexList() { return m_IndexList; }
	protected:
		StdVector<int> m_IndexList;
	};

	class IndexIterator
	{
	private:
		int m_Length;
		int m_Start;
		int m_Current;
		StdVector<int> m_IndexList;
	public:
		void Init(int start);
		void Init(int length, int start);
		inline int GetStart() const { return m_Start; }
		inline int Current() const { return m_Current; }
		bool MoveNext();
		int GetIndex(int input) const;
		void SetIndexList(const StdVector<int>& indexlist) { m_IndexList = indexlist; }
	};

	struct xml_string_writer : pugi::xml_writer
	{
		std::string result;

		virtual void write(const void* data, size_t size)
		{
			result.append(static_cast<const char*>(data), size);
		}

		static std::string node_to_string(pugi::xml_node node)
		{
			xml_string_writer writer;
			node.print(writer);

			return writer.result;
		}
	};

	template<typename T1, typename T2>  
	class TypeTypeMap
	{
	public:
		typedef std::pair<T1, T2> Pair;
		TypeTypeMap() {}
		TypeTypeMap(const std::initializer_list<Pair>& list)
			: m_Datas(list)
		{
		}

		bool Contains(T1 t1, T2 t2)
		{
			return m_Datas.count(Pair(t1, t2)) > 0;
		}
	protected:
		std::set<Pair> m_Datas;
	};
}

#endif