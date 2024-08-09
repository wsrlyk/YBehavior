#include "YBehavior/sharedvariablecreatehelper.h"

namespace YBehavior
{
	DataCreateHelperMgr::HelperMapType DataCreateHelperMgr::_Helpers;
	YBehavior::IDataCreateHelper* DataCreateHelperMgr::_HelperList[MAX_TYPE_KEY];

	DataCreateHelperMgr::Constructor DataCreateHelperMgr::cons;
}
