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
		INT CreateIndexByName(const STRING& name);

#ifdef DEBUGGER
		template<typename T>
		const STRING& GetNameByIndex(const STRING& treeName, INT index);

		const STRING& GetNameByIndex(const STRING& treeName, INT index, INT typeNumberId);
#endif
	public:
		struct YBEHAVIOR_API NameIndexInfo
		{
			std::unordered_map<STRING, INT> mNameHash;
			INT mNameIndex;

			NameIndexInfo()
			{
				mNameIndex = 0;
			}

			void Reset()
			{
				mNameHash.clear();
				mNameIndex = 0;
			}
		};

		struct YBEHAVIOR_API NameIndexMgr
		{
		private:
			std::unordered_map<int, NameIndexInfo> m_Infos;
		public:
			void Reset()
			{
				for (auto it = m_Infos.begin(); it != m_Infos.end(); ++it)
					it->second.Reset();
			}

			NameIndexInfo& Get(int index)
			{
				return m_Infos[index];
			}

			void AssignIndex(const NameIndexMgr& other)
			{
				for (auto it = other.m_Infos.begin(); it != other.m_Infos.end(); ++it)
				{
					m_Infos[it->first].mNameIndex = it->second.mNameIndex;
				}
			}
		};
	private:
		//static std::map<std::string, NameIndexInfo> mNameIndexInfos;
		NameIndexMgr mCommonNameIndexInfo;
		NameIndexMgr mTempNameIndexInfo;
		NameIndexMgr* mpCurActiveNameIndexInfo;
		STRING mCurActiveTreeName;

#ifdef DEBUGGER
		typedef std::unordered_map<INT, STRING> IndexNameMapType;
		IndexNameMapType mCommonIndexNameMap;
		std::unordered_map<STRING, IndexNameMapType> mIndexNameMap;
		IndexNameMapType* mpCurActiveIndexNameMap;
#endif
	};

	template<typename T>
	INT NodeFactory::CreateIndexByName(const STRING& name)
	{
		int typeNumberId = GetClassTypeNumberId<T>();

		NameIndexInfo& commonNameIndexInfo = mCommonNameIndexInfo.Get(typeNumberId);

		auto it = commonNameIndexInfo.mNameHash.find(name);
		if (it != commonNameIndexInfo.mNameHash.end())
			return it->second;

		if (mpCurActiveNameIndexInfo == NULL)
			return -1;

		NameIndexInfo& curActiveNameIndexInfo = mpCurActiveNameIndexInfo->Get(typeNumberId);
		///> common已经找过，不用再找一遍
		if (mpCurActiveNameIndexInfo == &mCommonNameIndexInfo || curActiveNameIndexInfo.mNameHash.find(name) == curActiveNameIndexInfo.mNameHash.end())
		{
			INT index = curActiveNameIndexInfo.mNameIndex++;
			LOG_BEGIN << "ADD node: " << name << "index: " << index << LOG_END;
			curActiveNameIndexInfo.mNameHash[name] = index;

#ifdef DEBUGGER
			(*mpCurActiveIndexNameMap)[typeNumberId << 16 | index] = name;
#endif
		}

		return curActiveNameIndexInfo.mNameHash[name];
	}

#ifdef DEBUGGER
	template<typename T>
	const STRING& NodeFactory::GetNameByIndex(const STRING& treeName, INT index)
	{
		return GetNameByIndex(treeName, index, GetClassTypeNumberId<T>());
	}
#endif
}

#endif