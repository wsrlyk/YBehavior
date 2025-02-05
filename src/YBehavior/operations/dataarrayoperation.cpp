#include "dataarrayoperation.h"

namespace YBehavior
{
	DataArrayOperationMgr::DataArrayOperationMgr()
	{
#define REGISTER_ARRAYOPERATION(T)\
	{\
		auto key = GetTypeID<StdVector<T>>();\
		m_Operations[key] = new DataArrayOperationHelper<StdVector<T>>();\
	}

		REGISTER_ARRAYOPERATION(INT);
		REGISTER_ARRAYOPERATION(FLOAT);
		REGISTER_ARRAYOPERATION(STRING);
		REGISTER_ARRAYOPERATION(Vector3);
		REGISTER_ARRAYOPERATION(ULONG);
		REGISTER_ARRAYOPERATION(EntityWrapper);
		REGISTER_ARRAYOPERATION(BOOL);
	}
	DataArrayOperationMgr::~DataArrayOperationMgr()
	{
		for (auto it = m_Operations.begin(); it != m_Operations.end(); ++it)
		{
			delete it->second;
		}
		m_Operations.clear();
	}
	const IDataArrayOperationHelper* DataArrayOperationMgr::Get(TYPEID t) const
	{
		auto it = m_Operations.find(t);
		if (it != m_Operations.end())
			return it->second;
		return nullptr;
	}
}
