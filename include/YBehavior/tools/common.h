#ifndef _YBEHAVIOR_COMMON_H_
#define _YBEHAVIOR_COMMON_H_

#include <vector>
#include "YBehavior/3rd/pugixml/pugixml.hpp"

namespace YBehavior
{
	class RandomIndex
	{
	public:
		void Rand();
		void Append() { m_IndexList.push_back(m_IndexList.size()); }
		int operator[] (int index);
		const std::vector<int>& GetIndexList() { return m_IndexList; }
	protected:
		std::vector<int> m_IndexList;
	};

	class IndexIterator
	{
	private:
		int m_Length;
		int m_Start;
		std::vector<int> m_IndexList;
	public:
		void Init(int start);
		inline int GetStart() const { return m_Start; }
		int GetIndex(int input) const;
		void SetIndexList(const std::vector<int>& indexlist) { m_IndexList = indexlist; }
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
}

#endif