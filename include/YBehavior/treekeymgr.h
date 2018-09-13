#ifndef _YBEHAVIOR_TREEKEYMGR_H_
#define _YBEHAVIOR_TREEKEYMGR_H_

#include "YBehavior/types.h"
#include "utility.h"
#include "singleton.h"

namespace YBehavior
{
	struct YBEHAVIOR_API NameKeyInfo
	{
		std::unordered_map<STRING, KEY> mNameHash;
#ifdef DEBUGGER
		std::unordered_map<KEY, STRING> mReverseNameHash;
#endif
		KEY mKeyCounter;

		NameKeyInfo()
		{
			mKeyCounter = 0;
		}

		void Reset()
		{
			mNameHash.clear();
#ifdef DEBUGGER
			mReverseNameHash.clear();
#endif
			mKeyCounter = 0;
		}

		KEY Get(const STRING& s)
		{
			auto it = mNameHash.find(s);
			if (it != mNameHash.end())
				return it->second;
			return Utility::INVALID_KEY;
		}
#ifdef DEBUGGER
		const STRING& Get(const KEY& key)
		{
			auto it = mReverseNameHash.find(key);
			if (it != mReverseNameHash.end())
				return it->second;
			return Utility::StringEmpty;
		}
#endif
	};

	struct YBEHAVIOR_API NameKeyMgr
	{
	private:
		std::unordered_map<TYPEID, NameKeyInfo> m_Infos;
	public:
		void Reset()
		{
			for (auto it = m_Infos.begin(); it != m_Infos.end(); ++it)
				it->second.Reset();
		}

		NameKeyInfo& Get(TYPEID typeID)
		{
			return m_Infos[typeID];
		}

		void AssignKey(const NameKeyMgr& other)
		{
			for (auto it = other.m_Infos.begin(); it != other.m_Infos.end(); ++it)
			{
				m_Infos[it->first].mKeyCounter = it->second.mKeyCounter;
			}
		}

		template<typename T>
		KEY GetKey(const STRING& name)
		{
			TYPEID typeNumberId = GetClassTypeNumberId<T>();
			NameKeyInfo& info = Get(typeNumberId);
			return info.Get(name);
		}

#ifdef DEBUGGER
		template<typename T>
		const STRING& GetName(const KEY& key)
		{
			TYPEID typeNumberId = GetClassTypeNumberId<T>();
			NameKeyInfo& info = Get(typeNumberId);
			return info.Get(key);
		}
#endif
	};

	class YBEHAVIOR_API TreeKeyMgr: public Singleton<TreeKeyMgr>
	{
	public:
		TreeKeyMgr();
		///> All infos now are stored in common info, so this function is disabled.
		//void SetActiveTree(NameKeyMgr* nameKeyMgr, bool bReset);
		template<typename T>
		KEY CreateKeyByName(const STRING& name);
		template<typename T>
		KEY GetKeyByName(const STRING& name);
		KEY GetKeyByName(const STRING& name, TYPEID typeID);

#ifdef DEBUGGER
		template<typename T>
		const STRING& GetNameByKey(KEY key);
		const STRING& GetNameByKey(KEY key, TYPEID typeID);
#endif
	private:
		NameKeyMgr mCommonNameKeyInfo;
		NameKeyMgr* mpCurActiveNameKeyInfo;
	};

	template<typename T>
	KEY TreeKeyMgr::GetKeyByName(const STRING& name)
	{
		return GetKeyByName(name, GetClassTypeNumberId<T>());
	}

#ifdef DEBUGGER
	template<typename T>
	const STRING& TreeKeyMgr::GetNameByKey(KEY key)
	{
		return GetNameByKey(key, GetClassTypeNumberId<T>());
	}
#endif

	template<typename T>
	KEY TreeKeyMgr::CreateKeyByName(const STRING& name)
	{
		KEY key = mCommonNameKeyInfo.GetKey<T>(name);
		if (key != Utility::INVALID_KEY)
			return key;
		if (mpCurActiveNameKeyInfo == NULL)
			return Utility::INVALID_KEY;

		TYPEID typeNumberId = GetClassTypeNumberId<T>();
		NameKeyInfo& curActiveNameKeyInfo = mpCurActiveNameKeyInfo->Get(typeNumberId);

		if (mpCurActiveNameKeyInfo == &mCommonNameKeyInfo || curActiveNameKeyInfo.Get(name) == Utility::INVALID_KEY)
		{
			KEY key = curActiveNameKeyInfo.mKeyCounter++;
#ifdef PRINT_INTERMEDIATE_INFO
			LOG_BEGIN << "ADD node: " << name << "key: " << key << LOG_END;
#endif
			curActiveNameKeyInfo.mNameHash[name] = key;

#ifdef DEBUGGER
			curActiveNameKeyInfo.mReverseNameHash[key] = name;
#endif
		}

		return curActiveNameKeyInfo.mNameHash[name];
	}
}

#endif