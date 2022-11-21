#include "YBehavior/variables/variablecompare.h"

namespace YBehavior
{
	VariableCompareMgr::VariableCompareMgr()
	{
#define REGISTER_COMPARE(T)\
	{\
		auto key = GetTypeID<T>();\
		m_Compares[key] = new VariableCompareHelper<T>();\
	}

		REGISTER_COMPARE(INT);
		REGISTER_COMPARE(FLOAT);
		REGISTER_COMPARE(STRING);
		REGISTER_COMPARE(Vector3);
		REGISTER_COMPARE(ULONG);
		REGISTER_COMPARE(EntityWrapper);
		REGISTER_COMPARE(BOOL);

	}
	VariableCompareMgr::~VariableCompareMgr()
	{
		for (auto it = m_Compares.begin(); it != m_Compares.end(); ++it)
		{
			delete it->second;
		}
		m_Compares.clear();
	}
	const IVariableCompareHelper* VariableCompareMgr::Get(TYPEID t) const
	{
		auto it = m_Compares.find(t);
		if (it != m_Compares.end())
			return it->second;
		return nullptr;
	}
}
