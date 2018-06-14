#include "YBehavior/registerdata.h"
#include "YBehavior/utility.h"

YBehavior::RegisterData::RegisterData()
{
	m_bDirty = false;
}

void YBehavior::RegisterData::Clear()
{
	m_VecInt.clear();
	m_VecFloat.clear();
	m_VecString.clear();
	m_VecUlong.clear();
	m_VecBool.clear();

	m_bDirty = false;
	m_Event = Utility::StringEmpty;
}
