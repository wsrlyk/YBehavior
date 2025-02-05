#include "dataunaryop.h"

namespace YBehavior
{
	DataUnaryOpMgr::DataUnaryOpMgr()
	{
#define REGISTER_OP_1(T)\
	{\
		m_UnaryOps[GetTypeID<T>()] = new DataUnaryOpHelper<T>();\
	}

		REGISTER_OP_1(INT);
		REGISTER_OP_1(FLOAT);
	}
	DataUnaryOpMgr::~DataUnaryOpMgr()
	{
		for (auto it = m_UnaryOps.begin(); it != m_UnaryOps.end(); ++it)
		{
			delete it->second;
		}
		m_UnaryOps.clear();
	}
	const IDataUnaryOpHelper* DataUnaryOpMgr::Get(TYPEID t) const
	{
		auto it = m_UnaryOps.find(t);
		if (it != m_UnaryOps.end())
			return it->second;
		return nullptr;
	}
}
