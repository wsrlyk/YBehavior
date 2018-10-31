#ifndef _YBEHAVIOR_MEMORY_H_
#define _YBEHAVIOR_MEMORY_H_

#include "YBehavior/shareddataex.h"
#include <stack>

namespace YBehavior
{
	class YBEHAVIOR_API Memory
	{
	public:
		Memory();
		~Memory();
		inline SharedDataEx* GetMainData() { return m_pMainData; }
		SharedDataEx* GetStackTop();

		void Push(SharedDataEx* pTemplate);
		void Pop();
	protected:
		SharedDataEx* m_pMainData;
		std::stack<SharedDataEx*> m_Stack;
	};
}

#endif