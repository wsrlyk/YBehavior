#ifndef _YBEHAVIOR_TREEKEYMGR_H_
#define _YBEHAVIOR_TREEKEYMGR_H_

#include "YBehavior/types/types.h"
#include "singleton.h"
#include <map>

namespace YBehavior
{
	class TreeKeyMgr : public Singleton<TreeKeyMgr>
	{
	public:
		KEY CreateKeyByName(const STRING& name);
		KEY GetKeyByName(const STRING& name) const;

#ifdef YDEBUGGER
		const STRING& GetNameByKey(KEY key) const;
#endif
	private:
		std::map<STRING, KEY> m_Name2Hash;
#ifdef YDEBUGGER
		std::map<KEY, STRING> m_Hash2Name;
#endif
	};

}
#endif