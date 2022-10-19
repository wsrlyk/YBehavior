#ifndef _YBEHAVIOR_MEMORY_H_
#define _YBEHAVIOR_MEMORY_H_

#include "YBehavior/shareddataex.h"
#include <vector>

namespace YBehavior
{
	class BehaviorTree;
	class SharedDataEx;
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

	///> Deque has poor performance at traversing;
	///> List will new/delete nodes when push/pop;
	///> Stack cant traverse.
	///> So we choose vector. 
	typedef std::vector<StackInfo> MemoryStack;
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
		TempMemory() {}
		TempMemory(SharedDataEx* pMain, SharedDataEx* pLocal);
		void Set(SharedDataEx* pMain, SharedDataEx* pLocal);
		SharedDataEx* GetMainData() override { return m_pMainData; }
		SharedDataEx* GetStackTop() override { return m_pLocalData; }
	private:
		SharedDataEx* m_pMainData{};
		SharedDataEx* m_pLocalData{};
	};
}

#endif