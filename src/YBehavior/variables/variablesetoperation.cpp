#include "YBehavior/variables/variablesetoperation.h"

namespace YBehavior
{
	VariableSetOperationMgr::VariableSetOperationMgr()
	{
#define REGISTER_SETOPERATION(T)\
	{\
		auto key = GetTypeID<StdVector<T>>();\
		m_SetOperations[key] = new VariableSetOperationHelper<StdVector<T>>();\
	}

		REGISTER_SETOPERATION(INT);
		REGISTER_SETOPERATION(FLOAT);
		REGISTER_SETOPERATION(STRING);
		REGISTER_SETOPERATION(Vector3);
		REGISTER_SETOPERATION(ULONG);
		REGISTER_SETOPERATION(EntityWrapper);
		REGISTER_SETOPERATION(BOOL);
	}
	VariableSetOperationMgr::~VariableSetOperationMgr()
	{
		for (auto it = m_SetOperations.begin(); it != m_SetOperations.end(); ++it)
		{
			delete it->second;
		}
		m_SetOperations.clear();
	}
	const IVariableSetOperationHelper* VariableSetOperationMgr::Get(TYPEID t) const
	{
		auto it = m_SetOperations.find(t);
		if (it != m_SetOperations.end())
			return it->second;
		return nullptr;
	}
}
