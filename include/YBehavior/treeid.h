#ifndef _YBEHAVIOR_TREEID_H_
#define _YBEHAVIOR_TREEID_H_

#include "YBehavior/types.h"
#include <unordered_map>
#include <unordered_set>

namespace YBehavior
{
	class TreeID
	{
		STRING m_TreeName;

		std::unordered_map<STRING, STRING>* m_pAllSubs;

		std::unordered_map<STRING, STRING> m_SubsMap;
		std::vector<STRING> m_SubsList;
		std::unordered_set<STRING> m_DontHave;

		UINT m_ID;

	public:
		TreeID(const STRING& name);
		void SetSubTrees(const std::vector<STRING>& inputs);
		bool TryGetSubTreeName(const STRING& id, const STRING& defaultName, STRING& outName);
		void BuildID();
		inline UINT GetID() const { return m_ID; }
		inline const STRING& GetName() const { return m_TreeName; }

		bool operator< (const TreeID& other) const;
		bool operator== (const TreeID& other) const;

		bool IsSameTree(const STRING& name, const std::vector<STRING>* subs) const;
	};
}

#endif