#include "YBehavior/sharedvariablecreatehelper.h"

namespace YBehavior
{
	SharedVariableCreateHelperMgr::HelperMapType SharedVariableCreateHelperMgr::_Helpers;
	YBehavior::ISharedVariableCreateHelper* SharedVariableCreateHelperMgr::_HelperList[MAX_TYPE_KEY];

	SharedVariableCreateHelperMgr::Constructor SharedVariableCreateHelperMgr::cons;
}
