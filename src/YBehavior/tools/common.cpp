#include "YBehavior/tools/common.h"
#include "YBehavior/utility.h"
#include "YBehavior/logger.h"

namespace YBehavior
{

	void RandomIndex::Rand()
	{
		for (int i = m_IndexList.size() - 1; i > 0; --i)
		{
			int j = Utility::Rand(0, i + 1);
			if (j == i)
				continue;
			int temp = m_IndexList[j];
			m_IndexList[j] = m_IndexList[i];
			m_IndexList[i] = temp;
		}
	}

	int RandomIndex::operator[](int index)
	{
		if (index < 0 || (size_t)index >= m_IndexList.size())
		{
			ERROR_BEGIN << "Random index out of range, try get " << index << " but length = " << m_IndexList.size() << ERROR_END;
		}

		return m_IndexList[index];
	}

	void IndexIterator::Init(int start)
	{
		m_Start = start;
		m_IndexList.clear();
	}

	int IndexIterator::GetIndex(int input) const
	{
		if (input >= (int)m_IndexList.size() || input < 0)
			return input;
		return m_IndexList[input];
	}

}
