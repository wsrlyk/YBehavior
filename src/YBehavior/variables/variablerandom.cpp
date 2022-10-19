#include "YBehavior/variables/variablerandom.h"

namespace YBehavior
{
	VariableRandomMgr::VariableRandomMgr()
	{
#define REGISTER_RANDOM(T)\
	{\
		auto key = GetTypeID<T>();\
		m_Randoms[key] = new VariableRandomHelper<T>();\
	}

		REGISTER_RANDOM(INT);
		REGISTER_RANDOM(FLOAT);
		REGISTER_RANDOM(BOOL);

	}
	VariableRandomMgr::~VariableRandomMgr()
	{
		for (auto it = m_Randoms.begin(); it != m_Randoms.end(); ++it)
		{
			delete it->second();
		}
		m_Randoms.clear();
	}
	const IVariableRandomHelper* VariableRandomMgr::Get(TYPEID t) const
	{
		auto it = m_Randoms.find(t);
		if (it != m_Randoms.end())
			return it->second();
		return nullptr;
	}
}
