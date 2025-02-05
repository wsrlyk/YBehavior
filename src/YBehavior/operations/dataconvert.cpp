#include "dataconvert.h"

namespace YBehavior
{
	DataConvertMgr::DataConvertMgr()
	{
#define REGISTER_CONVERTOR(FromType, ToType)\
	{\
		auto key = std::make_pair(GetTypeID<FromType>(), GetTypeID<ToType>());\
		m_Converts[key] = new DataConvertHelper<FromType, ToType>();\
	}

		REGISTER_CONVERTOR(INT, FLOAT);
		REGISTER_CONVERTOR(FLOAT, INT);
		REGISTER_CONVERTOR(INT, BOOL);
		REGISTER_CONVERTOR(BOOL, INT);

		REGISTER_CONVERTOR(INT, STRING);
		REGISTER_CONVERTOR(FLOAT, STRING);
		REGISTER_CONVERTOR(BOOL, STRING);

		REGISTER_CONVERTOR(STRING, INT);
		REGISTER_CONVERTOR(STRING, FLOAT);
		REGISTER_CONVERTOR(STRING, BOOL);
	}
	DataConvertMgr::~DataConvertMgr()
	{
		for (auto it = m_Converts.begin(); it != m_Converts.end(); ++it)
		{
			delete it->second;
		}
		m_Converts.clear();
	}
	const IDataConvertHelper* DataConvertMgr::GetConvert(TYPEID from, TYPEID to) const
	{
		auto it = m_Converts.find(std::make_pair(from, to));
		if (it != m_Converts.end())
			return it->second;
		return nullptr;
	}
}
