#ifndef _YBEHAVIOR_MEMORY_H_
#define _YBEHAVIOR_MEMORY_H_

#include "YBehavior/shareddataex.h"
#include <stack>
#include <list>

namespace YBehavior
{
	class BehaviorTree;
	struct StackInfo
	{
		StackInfo();
		StackInfo(BehaviorTree* pTree);
		StackInfo(StackInfo&& other);
		StackInfo(const StackInfo& other);
		StackInfo& operator=(const StackInfo& other);
		~StackInfo();
		BehaviorTree* Owner;
		SharedDataEx* Data;
	private:
		ObjectPool<SharedDataEx> *m_DataPool;
	};

	typedef std::list<StackInfo> MemoryStack;
	class Memory : public IMemory
	{
	public:
		Memory();
		~Memory();
		SharedDataEx* GetMainData() override { return m_pMainData; }
		SharedDataEx* GetStackTop() override;
		const StackInfo* GetStackTopInfo();

		inline MemoryStack& GetStack() { return m_Stack; }

		void Push(BehaviorTree* pTree);
		void Pop();
	protected:
		SharedDataEx* m_pMainData;
		MemoryStack m_Stack;
	};

	class TempMemory : public IMemory
	{
	public:
		TempMemory(SharedDataEx* pMain, SharedDataEx* pLocal);
		SharedDataEx* GetMainData() override { return m_pMainData; }
		SharedDataEx* GetStackTop() override { return m_pLocalData; }
	private:
		SharedDataEx* m_pMainData;
		SharedDataEx* m_pLocalData;
	};
}

#endif