#include "YBehavior/operations/dataoperation.h"

namespace YBehavior
{
	DataOperationMgr::DataOperationMgr()
	{
#define REGISTER_OPERATION(T)\
	{\
		auto key = GetTypeID<T>();\
		m_Operations[key] = new DataOperationHelper<T>();\
	}

		REGISTER_OPERATION(INT);
		REGISTER_OPERATION(FLOAT);
		REGISTER_OPERATION(BOOL);
		REGISTER_OPERATION(ULONG);
		REGISTER_OPERATION(Vector3);
		REGISTER_OPERATION(EntityWrapper);
		REGISTER_OPERATION(STRING);

	}
	DataOperationMgr::~DataOperationMgr()
	{
		for (auto it = m_Operations.begin(); it != m_Operations.end(); ++it)
		{
			delete it->second;
		}
		m_Operations.clear();
	}
	const IDataOperationHelper* DataOperationMgr::Get(TYPEID t) const
	{
		auto it = m_Operations.find(t);
		if (it != m_Operations.end())
			return it->second;
		return nullptr;
	}

	TempObject::~TempObject()
	{
		if (pData && pHelper)
			pHelper->RecycleData(pData);
	}

}
