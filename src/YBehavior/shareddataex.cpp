#include "YBehavior/shareddataex.h"

namespace YBehavior
{
	SharedDataEx::SharedDataEx()
	{
#define YBEHAVIOR_CREATE_SHAREDDATA_ARRAY(T)\
		m_Datas[GetTypeKey<T>()] = new DataArray<T>();

		FOR_EACH_TYPE(YBEHAVIOR_CREATE_SHAREDDATA_ARRAY);
	}

	SharedDataEx::SharedDataEx(SharedDataEx&& other)
	{
#define YBEHAVIOR_MOVE_SHAREDDATA_ARRAY(T)\
		m_Datas[GetTypeKey<T>()] = other.m_Datas[GetTypeKey<T>()];\
		other.m_Datas[GetTypeKey<T>()] = nullptr;

		FOR_EACH_TYPE(YBEHAVIOR_CREATE_SHAREDDATA_ARRAY);
	}

	SharedDataEx::~SharedDataEx()
	{
		for (KEY i = 0; i < MAX_TYPE_KEY; ++i)
		{
			if (m_Datas[i] != nullptr)
				delete m_Datas[i];
		}
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

}
