#include "datacompare.h"

namespace YBehavior
{
	DataCompareMgr::DataCompareMgr()
	{
#define REGISTER_COMPARE(T)\
	{\
		auto key = GetTypeID<T>();\
		m_Compares[key] = new DataCompareHelper<T>();\
	}

		REGISTER_COMPARE(INT);
		REGISTER_COMPARE(FLOAT);
		REGISTER_COMPARE(STRING);
		REGISTER_COMPARE(Vector3);
		REGISTER_COMPARE(ULONG);
		REGISTER_COMPARE(EntityWrapper);
		REGISTER_COMPARE(BOOL);

	}
	DataCompareMgr::~DataCompareMgr()
	{
		for (auto it = m_Compares.begin(); it != m_Compares.end(); ++it)
		{
			delete it->second;
		}
		m_Compares.clear();
	}
	const IDataCompareHelper* DataCompareMgr::Get(TYPEID t) const
	{
		auto it = m_Compares.find(t);
		if (it != m_Compares.end())
			return it->second;
		return nullptr;
	}
}
