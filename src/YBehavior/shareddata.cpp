#include "YBehavior/shareddata.h"

namespace YBehavior
{
	FOR_EACH_TYPE_WITH_VALUE(DEFINE_DEFAULT_TYPES_V);
	const Vector3 Vector3::zero(0, 0, 0);





#define DEFINE_SHARED_TYPES(T)\
	Shared##T::Shared##T()\
	{\
		m_Index = SharedData::INVALID_INDEX;\
		m_Value = SharedData::s_Default##T;\
	}\
	const T& Shared##T::GetValue(SharedData* data)\
	{\
		if (data == nullptr || m_Index == SharedData::INVALID_INDEX)\
			return m_Value;\
		return data->Get##T(m_Index);\
	}\
	bool Shared##T::SetValue(SharedData* data, const T& v)\
	{\
		if (data == nullptr || m_Index == SharedData::INVALID_INDEX)\
		{\
			m_Value = v;\
			return true;\
		}\
		return data->Set(m_Index, v);\
	}\
	bool Shared##T::SetValue(SharedData* data, T&& v)\
	{\
		if (data == nullptr || m_Index == SharedData::INVALID_INDEX)\
		{\
			m_Value = v;\
			return true;\
		}\
		return data->Set(m_Index, std::move(v));\
	}

	FOR_EACH_TYPE(DEFINE_SHARED_TYPES);

	void SharedData::Clone(const SharedData& other)
	{
#define CLONE_VARIABLES(T)\
	if (m_##T##s)\
		m_##T##s->clear();\
	if (other.m_##T##s)\
	{\
		if (!m_##T##s)\
			m_##T##s = new std::vector<T>(other.m_##T##s->size());\
		*m_##T##s = (*other.m_##T##s);\
	}

	FOR_EACH_TYPE(CLONE_VARIABLES);
	}
}