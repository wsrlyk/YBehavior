#include "YBehavior/treekeymgr.h"
namespace YBehavior
{

	KEY TreeKeyMgr::CreateKeyByName(const STRING& name)
	{
		auto it = m_Name2Hash.find(name);
		if (it != m_Name2Hash.end())
			return it->second;
		KEY key = (KEY)m_Name2Hash.size() + 1;
		m_Name2Hash[name] = key;
#ifdef YDEBUGGER
		m_Hash2Name[key] = name;
#endif
		return key;
	}

	KEY TreeKeyMgr::GetKeyByName(const STRING& name) const
	{
		auto it = m_Name2Hash.find(name);
		if (it != m_Name2Hash.end())
			return it->second;
		return Utility::INVALID_KEY;
	}

#ifdef YDEBUGGER
	const YBehavior::STRING& TreeKeyMgr::GetNameByKey(KEY key) const
	{
		auto it = m_Hash2Name.find(key);
		if (it != m_Hash2Name.end())
			return it->second;
		return Utility::StringEmpty;
	}
#endif
}
