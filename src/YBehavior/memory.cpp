#include "YBehavior/memory.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	StackInfo::StackInfo()
		: Owner(nullptr)
		, Data(nullptr)
	{

	}


	StackInfo::StackInfo(BehaviorTree* pTree)
	{
		Owner = pTree;
		if (pTree && pTree->GetLocalDataIfExists())
		{
			Data = ObjectPool<SharedDataEx>::Get();
			Data->CloneFrom(*pTree->GetLocalDataIfExists());
		}
		else
		{
			Data = nullptr;
		}
	}

	StackInfo::StackInfo(StackInfo&& other)
	{
		Owner = other.Owner;
		Data = other.Data;

		other.Data = nullptr;
		other.Owner = nullptr;
	}

	StackInfo::StackInfo(const StackInfo& other)
	{
		Owner = other.Owner;
		if (other.Data)
		{
			Data = ObjectPool<SharedDataEx>::Get();
			Data->CloneFrom(*other.Data);
		}
		else
		{
			Data = nullptr;
		}
	}

	StackInfo::~StackInfo()
	{
		if (Data)
			ObjectPool<SharedDataEx>::Recycle(Data);;
	}

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

		m_Stack.clear();
	}

	YBehavior::SharedDataEx* Memory::GetStackTop()
	{
		if (m_Stack.empty())
			return nullptr;

		return m_Stack.back().Data;
	}

	const YBehavior::StackInfo* Memory::GetStackTopInfo()
	{
		if (m_Stack.empty())
			return nullptr;

		return &m_Stack.back();
	}

	void Memory::Push(BehaviorTree* pTree)
	{
		StackInfo info(pTree);
		m_Stack.push_back(std::move(info));
	}

	void Memory::Pop()
	{
		if (m_Stack.empty())
			return;

		//auto it = m_Stack.top();
		//delete it.Data;
		m_Stack.pop_back();
	}

	TempMemory::TempMemory(SharedDataEx* pMain, SharedDataEx* pLocal)
		: m_pMainData(pMain)
		, m_pLocalData(pLocal)
	{

	}

}
