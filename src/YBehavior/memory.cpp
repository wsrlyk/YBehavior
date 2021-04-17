#include "YBehavior/memory.h"
#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	StackInfo::StackInfo()
		: Owner(nullptr)
		, Data(nullptr)
		, m_DataPool(nullptr)
	{

	}


	StackInfo::StackInfo(BehaviorTree* pTree)
	{
		Owner = pTree;
		if (pTree && pTree->GetLocalDataIfExists())
		{
			m_DataPool = &pTree->GetLocalDataPool();
			Data = m_DataPool->Fetch();
			Data->MergeFrom(*pTree->GetLocalDataIfExists(), false);
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
		m_DataPool = other.m_DataPool;

		other.Data = nullptr;
		other.Owner = nullptr;
	}

	StackInfo::StackInfo(const StackInfo& other)
	{
		Owner = other.Owner;
		if (other.Data && other.m_DataPool != nullptr)
		{
			m_DataPool = other.m_DataPool;
			Data = m_DataPool->Fetch();
			Data->MergeFrom(*other.Data, false);
		}
		else
		{
			Data = nullptr;
		}
	}

	StackInfo& StackInfo::operator=(const StackInfo& other)
	{
		Owner = other.Owner;
		if (other.Data && other.m_DataPool != nullptr)
		{
			m_DataPool = other.m_DataPool;
			Data = m_DataPool->Fetch();
			Data->MergeFrom(*other.Data, false);
		}
		else
		{
			Data = nullptr;
		}
		return *this;
	}

	StackInfo::~StackInfo()
	{
		if (Data)
		{
			if (m_DataPool)
				m_DataPool->Return(Data);
			else
				delete Data;
		}
	}

	Memory::Memory()
	{
		m_pMainData = new SharedDataEx();
	}

	Memory::~Memory()
	{
		delete m_pMainData;

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
