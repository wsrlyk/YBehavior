#include "YBehavior/shareddataex.h"

namespace YBehavior
{
#define DATAARRAY_ONETYPE_POINTER(T)\
		m_Datas[GetTypeKey<T>()] = &m_Data##T;

	SharedDataEx::SharedDataEx()
	{
		FOR_EACH_TYPE(DATAARRAY_ONETYPE_POINTER);
	}

	SharedDataEx::SharedDataEx(const SharedDataEx& other)
	{
#define DATAARRAY_COPYCONSTRUCTOR_ASSIGN(T)\
		m_Data##T = other.m_Data##T;

		FOR_EACH_TYPE(DATAARRAY_COPYCONSTRUCTOR_ASSIGN);
		FOR_EACH_TYPE(DATAARRAY_ONETYPE_POINTER);
	}

	SharedDataEx::~SharedDataEx()
	{
	}

	void SharedDataEx::CloneFrom(const SharedDataEx& other)
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			//if (m_Datas[i] != nullptr)
			//	delete m_Datas[i];

			//if (other.m_Datas[i] == nullptr)
			//{
			//	m_Datas[i] = nullptr;
			//}
			//else
			//{
			//	m_Datas[i] = other.m_Datas[i]->Clone();
			//}
			m_Datas[i]->CloneFrom(other.m_Datas[i]);
		}
	}

	void SharedDataEx::MergeFrom(const SharedDataEx& other, bool bOverride)
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			//if (other.m_Datas[i] == nullptr)
			//{
			//	continue;
			//}

			//if (m_Datas[i] == nullptr)
			//	m_Datas[i] = other.m_Datas[i]->Clone();
			//else
			//	m_Datas[i]->Merge(other.m_Datas[i], bOverride);
			m_Datas[i]->MergeFrom(other.m_Datas[i], bOverride);
		}
	}

	void SharedDataEx::Clear()
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			m_Datas[i]->Clear();
		}
	}

}
