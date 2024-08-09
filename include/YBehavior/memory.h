#ifndef _YBEHAVIOR_MEMORY_H_
#define _YBEHAVIOR_MEMORY_H_

#include "YBehavior/variable.h"
#include <vector>

namespace YBehavior
{
	class BehaviorTree;
	class VariableCollection;
	struct StackInfo
	{
		StackInfo();
		StackInfo(BehaviorTree* pTree);
		StackInfo(StackInfo&& other);
		StackInfo(const StackInfo& other);
		StackInfo& operator=(const StackInfo& other);
		~StackInfo();
		BehaviorTree* Owner;
		VariableCollection* Data;
	private:
		ObjectPool<VariableCollection> *m_DataPool;
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
		VariableCollection* GetMainData() override { return m_pMainData; }
		VariableCollection* GetStackTop() override;
		const StackInfo* GetStackTopInfo();

		inline MemoryStack& GetStack() { return m_Stack; }

		void Push(BehaviorTree* pTree);
		void Pop();
	protected:
		VariableCollection* m_pMainData;
		MemoryStack m_Stack;
	};

	class TempMemory : public IMemory
	{
	public:
		TempMemory() {}
		TempMemory(VariableCollection* pMain, VariableCollection* pLocal);
		void Set(VariableCollection* pMain, VariableCollection* pLocal);
		VariableCollection* GetMainData() override { return m_pMainData; }
		VariableCollection* GetStackTop() override { return m_pLocalData; }
	private:
		VariableCollection* m_pMainData{};
		VariableCollection* m_pLocalData{};
	};
}

#endif