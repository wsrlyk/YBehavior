#include "YBehavior/memory.h"

namespace YBehavior
{

	Memory::Memory()
	{
		m_pMainData = new SharedDataEx();
	}

	Memory::~Memory()
	{
		if (!m_pMainData)
		{
			delete m_pMainData;
			m_pMainData = nullptr;
		}

		while (!m_Stack.empty())
		{
			auto it = m_Stack.top();
			delete it;
			m_Stack.pop();
		}
	}

	YBehavior::SharedDataEx* Memory::GetStackTop()
	{
		if (m_Stack.empty())
			return nullptr;

		return m_Stack.top();
	}

	void Memory::Push(SharedDataEx* pTemplate)
	{
		SharedDataEx* cloned = nullptr;
		if (pTemplate)
		{
			cloned = new SharedDataEx();
			cloned->Clone(*pTemplate);
		}
		m_Stack.push(cloned);
	}

	void Memory::Pop()
	{
		if (m_Stack.empty())
			return;

		auto it = m_Stack.top();
		delete it;
		m_Stack.pop();
	}

}
