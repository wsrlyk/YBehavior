#ifndef _YBEHAVIOR_NODEFACTORY_H_
#define _YBEHAVIOR_NODEFACTORY_H_

#include "YBehavior/factory.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/logger.h"

namespace YBehavior
{
	class YBEHAVIOR_API NodeFactory: public Factory<BehaviorNode>
	{
	protected:
		static NodeFactory* s_NodeFactory;
	public:
		NodeFactory();
		static NodeFactory* Instance();
		void SetActiveTree(const STRING& tree);
		template<typename T>
		KEY CreateKeyByName(const STRING& name);

#ifdef DEBUGGER
		template<typename T>
		const STRING& GetNameByKey(const STRING& treeName, KEY key);

		const STRING& GetNameByKey(const STRING& treeName, KEY key, TYPEID typeNumberId);
#endif
	public:
		struct YBEHAVIOR_API NameKeyInfo
		{
			std::unordered_map<STRING, KEY> mNameHash;
			KEY mKeyCounter;

			NameKeyInfo()
			{
				mKeyCounter = 0;
			}

			void Reset()
			{
				mNameHash.clear();
				mKeyCounter = 0;
			}
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
		};
	private:
		NameKeyMgr mCommonNameKeyInfo;
		NameKeyMgr mTempNameKeyInfo;
		NameKeyMgr* mpCurActiveNameKeyInfo;
		STRING mCurActiveTreeName;

#ifdef DEBUGGER
		typedef std::unordered_map<KEY, STRING> KeyNameMapType;
		KeyNameMapType mCommonKeyNameMap;
		std::unordered_map<STRING, KeyNameMapType> mKeyNameMap;
		KeyNameMapType* mpCurActiveKeyNameMap;
#endif
	};

	template<typename T>
	KEY NodeFactory::CreateKeyByName(const STRING& name)
	{
		TYPEID typeNumberId = GetClassTypeNumberId<T>();

		NameKeyInfo& commonNameKeyInfo = mCommonNameKeyInfo.Get(typeNumberId);

		auto it = commonNameKeyInfo.mNameHash.find(name);
		if (it != commonNameKeyInfo.mNameHash.end())
			return it->second;

		if (mpCurActiveNameKeyInfo == NULL)
			return -1;

		NameKeyInfo& curActiveNameKeyInfo = mpCurActiveNameKeyInfo->Get(typeNumberId);

		if (mpCurActiveNameKeyInfo == &mCommonNameKeyInfo || curActiveNameKeyInfo.mNameHash.find(name) == curActiveNameKeyInfo.mNameHash.end())
		{
			KEY key = curActiveNameKeyInfo.mKeyCounter++;
			LOG_BEGIN << "ADD node: " << name << "key: " << key << LOG_END;
			curActiveNameKeyInfo.mNameHash[name] = key;

#ifdef DEBUGGER
			(*mpCurActiveKeyNameMap)[typeNumberId << 16 | key] = name;
#endif
		}

		return curActiveNameKeyInfo.mNameHash[name];
	}

#ifdef DEBUGGER
	template<typename T>
	const STRING& NodeFactory::GetNameByKey(const STRING& treeName, KEY key)
	{
		return GetNameByKey(treeName, key, GetClassTypeNumberId<T>());
	}
#endif
}

#endif