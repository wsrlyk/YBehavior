#ifndef _YBEHAVIOR_COMMON_H_
#define _YBEHAVIOR_COMMON_H_

#include <vector>

namespace YBehavior
{
	class RandomIndex
	{
	public:
		void Rand();
		void Append() { m_IndexList.push_back(m_IndexList.size()); }
		int operator[] (int index);
	protected:
		std::vector<int> m_IndexList;
	};
}

#endif