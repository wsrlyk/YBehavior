#include "datarandom.h"

namespace YBehavior
{
	DataRandomMgr::DataRandomMgr()
	{
#define REGISTER_RANDOM(T)\
	{\
		auto key = GetTypeID<T>();\
		m_Randoms[key] = new DataRandomHelper<T>();\
	}

		REGISTER_RANDOM(INT);
		REGISTER_RANDOM(FLOAT);
		REGISTER_RANDOM(BOOL);

	}
	DataRandomMgr::~DataRandomMgr()
	{
		for (auto it = m_Randoms.begin(); it != m_Randoms.end(); ++it)
		{
			delete it->second;
		}
		m_Randoms.clear();
	}
	const IDataRandomHelper* DataRandomMgr::Get(TYPEID t) const
	{
		auto it = m_Randoms.find(t);
		if (it != m_Randoms.end())
			return it->second;
		return nullptr;
	}
}
