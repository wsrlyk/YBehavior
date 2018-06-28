#ifndef _YBEHAVIOR_NODEFACTORY_H_
#define _YBEHAVIOR_NODEFACTORY_H_

#include "YBehavior/factory.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"

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
			return INVALID_KEY;
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

		template<typename T>
		const STRING& GetName(const KEY& key)
		{
			TYPEID typeNumberId = GetClassTypeNumberId<T>();
			NameKeyInfo& info = Get(typeNumberId);
			return info.Get(key);
		}
	};

	class YBEHAVIOR_API NodeFactory: public Factory<BehaviorNode>
	{
	protected:
		static NodeFactory* s_NodeFactory;
	public:
		NodeFactory();
		static NodeFactory* Instance();
		void SetActiveTree(NameKeyMgr* nameKeyMgr, bool bReset);
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
	KEY NodeFactory::GetKeyByName(const STRING& name)
	{
		return GetKeyByName(name, GetClassTypeNumberId<T>());
	}

#ifdef DEBUGGER
	template<typename T>
	const STRING& NodeFactory::GetNameByKey(KEY key)
	{
		return GetNameByKey(key, GetClassTypeNumberId<T>());
	}
#endif

	template<typename T>
	KEY NodeFactory::CreateKeyByName(const STRING& name)
	{
		KEY key = mCommonNameKeyInfo.GetKey<T>(name);
		if (key != INVALID_KEY)
			return key;
		if (mpCurActiveNameKeyInfo == NULL)
			return INVALID_KEY;

		TYPEID typeNumberId = GetClassTypeNumberId<T>();
		NameKeyInfo& curActiveNameKeyInfo = mpCurActiveNameKeyInfo->Get(typeNumberId);

		if (mpCurActiveNameKeyInfo == &mCommonNameKeyInfo || curActiveNameKeyInfo.Get(name) == INVALID_KEY)
		{
			KEY key = curActiveNameKeyInfo.mKeyCounter++;
			LOG_BEGIN << "ADD node: " << name << "key: " << key << LOG_END;
			curActiveNameKeyInfo.mNameHash[name] = key;

#ifdef DEBUGGER
			curActiveNameKeyInfo.mReverseNameHash[key] = name;
#endif
		}

		return curActiveNameKeyInfo.mNameHash[name];
	}
}

#endif