#include "YBehavior/shareddataex.h"

namespace YBehavior
{
	SharedDataEx::SharedDataEx()
	{
#define YBEHAVIOR_CREATE_SHAREDDATA_ARRAY(T)\
		m_Datas[GetTypeIndex<T>()] = new DataArray<T>();

		FOR_EACH_TYPE(YBEHAVIOR_CREATE_SHAREDDATA_ARRAY);
	}

}
