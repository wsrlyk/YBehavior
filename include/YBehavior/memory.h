#ifndef _YBEHAVIOR_MEMORY_H_
#define _YBEHAVIOR_MEMORY_H_

#include "YBehavior/shareddataex.h"
#include <stack>

namespace YBehavior
{
	class BehaviorTree;
	struct YBEHAVIOR_API StackInfo
	{
		StackInfo();
		StackInfo(BehaviorTree* pTree);
		StackInfo(StackInfo&& other);
		StackInfo(const StackInfo& other);
		~StackInfo();
		BehaviorTree* Owner;
		SharedDataEx* Data;
	};

	typedef std::list<StackInfo> MemoryStack;
	class YBEHAVIOR_API Memory
	{
	public:
		Memory();
		~Memory();
		inline SharedDataEx* GetMainData() { return m_pMainData; }
		SharedDataEx* GetStackTop();
		const StackInfo* GetStackTopInfo();

		inline MemoryStack& GetStack() { return m_Stack; }

		void Push(BehaviorTree* pTree);
		void Pop();
	protected:
		SharedDataEx* m_pMainData;
		MemoryStack m_Stack;
	};
}

#endif