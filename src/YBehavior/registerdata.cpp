#include "YBehavior/registerdata.h"
#include "YBehavior/utility.h"

YBehavior::RegisterData::RegisterData()
{
	m_bDirty = false;
#define VARIABLE_INIT(TYPE) m_ReceiveData.pVec##TYPE = &m_Vec##TYPE;
	///> m_ReceiveData.pVecInt = &m_VecInt;
	FOR_EACH_REGISTER_TYPE(VARIABLE_INIT);
	m_ReceiveData.pEvent = &m_Event;
}

void YBehavior::RegisterData::Clear()
{
#define VARIABLE_CLEAR(TYPE) m_Vec##TYPE.clear();
	///> m_VecInt.clear();
	FOR_EACH_REGISTER_TYPE(VARIABLE_CLEAR);

	m_bDirty = false;
	m_Event = Types::StringEmpty;
}
