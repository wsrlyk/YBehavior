#ifndef _YBEHAVIOR_BEHAVIORID_H_
#define _YBEHAVIOR_BEHAVIORID_H_

#include "YBehavior/types.h"
#include <unordered_map>
#include <unordered_set>

namespace YBehavior
{
	class BehaviorID
	{
		STRING m_TreeName;

		std::unordered_map<STRING, STRING>* m_pAllSubs;

		std::unordered_map<STRING, STRING> m_SubsMap;
		std::vector<STRING> m_SubsList;
		std::unordered_map<STRING, STRING> m_UseDefault;

		UINT m_ID;

	public:
		BehaviorID(const STRING& name);
		void SetMappings(const std::vector<STRING>& inputs);
		bool TryGet(const STRING& id, const STRING& defaultName, STRING& outName);
		void BuildID();
		void Merge(const YBehavior::BehaviorID& other);
		inline UINT GetID() const { return m_ID; }
		inline const STRING& GetName() const { return m_TreeName; }

		inline bool operator< (const BehaviorID& other) const { return m_ID < other.m_ID; }
		inline bool operator== (const BehaviorID& other) const { return m_ID == other.m_ID; }

		bool IsSame(const std::vector<STRING>* subs) const;
	};
}

#endif