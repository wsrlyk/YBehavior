#include "YBehavior/treekeymgr.h"
#include "YBehavior/logger.h"
namespace YBehavior
{
	void TreeKeyMgr::SetActiveTree(NameKeyMgr* nameKeyMgr, bool bReset)
	{
		//LOG_BEGIN << "SetActiveTree: " << tree.c_str() << LOG_END;

		if (nameKeyMgr == nullptr)
		{
			mpCurActiveNameKeyInfo = &mCommonNameKeyInfo;
			return;
		}
		else
		{
			mpCurActiveNameKeyInfo = nameKeyMgr;
		}

		if (bReset)
		{
			mpCurActiveNameKeyInfo->Reset();
			mpCurActiveNameKeyInfo->AssignKey(mCommonNameKeyInfo);
		}
	}

	KEY TreeKeyMgr::GetKeyByName(const STRING& name, TYPEID typeID)
	{
		NameKeyInfo& info = mCommonNameKeyInfo.Get(typeID);
		KEY key = info.Get(name);
		if (key != Utility::INVALID_KEY)
			return key;
		if (mpCurActiveNameKeyInfo == NULL)
			return Utility::INVALID_KEY;
		info = mpCurActiveNameKeyInfo->Get(typeID);
		return info.Get(name);
	}

	const STRING& TreeKeyMgr::GetNameByKey(KEY key, TYPEID typeID)
	{
		NameKeyInfo& info = mCommonNameKeyInfo.Get(typeID);
		const STRING& name = info.Get(key);
		if (name != Utility::StringEmpty)
			return name;
		if (mpCurActiveNameKeyInfo == NULL)
			return Utility::StringEmpty;
		info = mpCurActiveNameKeyInfo->Get(typeID);
		return info.Get(key);
	}

	TreeKeyMgr::TreeKeyMgr()
	{
		mCommonNameKeyInfo.Reset();
	}
}
