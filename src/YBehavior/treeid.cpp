#include "YBehavior/treeid.h"
#include "YBehavior/logger.h"
#include <algorithm>
#include "YBehavior/utility.h"
#include "YBehavior/tools/objectpool.h"

YBehavior::TreeID::TreeID(const STRING& name)
	: m_TreeName(name)
	, m_pAllSubs(nullptr)
{
}

void YBehavior::TreeID::SetSubTrees(const std::vector<STRING>& inputs)
{
	if (inputs.size() % 2 == 1)
	{
		ERROR_BEGIN << "Even amount of inputs is required. But now it's " << inputs.size() << ERROR_END;
		return;
	}

	if (!m_pAllSubs)
		m_pAllSubs = ObjectPool<std::unordered_map<STRING, STRING>>::Get();
	m_pAllSubs->clear();

	for (UINT i = 0; i < inputs.size(); i += 2)
	{
		(*m_pAllSubs)[inputs[i]] = inputs[i + 1];
	}

	m_SubsMap.clear();
	m_SubsList.clear();
}

bool YBehavior::TreeID::TryGetSubTreeName(const STRING& id, const STRING& defaultName, STRING& outName)
{
	auto it = m_SubsMap.find(id);
	///> Already added.
	if (it != m_SubsMap.end())
	{
		outName = it->second;
		return true;
	}

	outName = defaultName;
	///> No replaced subs
	if (!m_pAllSubs)
	{
		m_UseDefault[id] = defaultName;
		return false;
	}


	it = m_pAllSubs->find(id);
	///> No such an id
	if (it == m_pAllSubs->end())
	{
		m_UseDefault[id] = defaultName;
		return false;
	}

	///> Same as DefaultName
	if (it->second == defaultName)
	{
		m_UseDefault[id] = defaultName;
		return false;
	}

	///> Add
	m_SubsList.push_back(id);
	m_SubsMap[it->first] = it->second;
	outName = it->second;
	return true;
}

void YBehavior::TreeID::BuildID()
{
	if (m_pAllSubs)
	{
		m_pAllSubs->clear();
		ObjectPool<std::unordered_map<STRING, STRING>>::Recycle(m_pAllSubs);
		m_pAllSubs = nullptr;
	}

	sort(m_SubsList.begin(), m_SubsList.end());

	STRING s;
	s += m_TreeName;
	for (auto it = m_SubsList.begin(); it != m_SubsList.end(); ++it)
	{
		s += *it;
		s += m_SubsMap[*it];
	}

	m_ID = Utility::Hash(s);
}

bool YBehavior::TreeID::operator<(const TreeID& other) const
{
	return m_ID < other.m_ID;
}

bool YBehavior::TreeID::operator==(const TreeID& other) const
{
	return m_ID == other.m_ID;
}

bool YBehavior::TreeID::IsSameTree(const STRING& name, const std::vector<STRING>* subs) const
{
	///> If there're some subs that have been changed
	if (subs == nullptr)
	{
		return  m_SubsList.size() == 0;
	}

	if (subs->size() % 2 == 1)
	{
		ERROR_BEGIN << "Even amount of subs is required. But now it's " << subs->size() << ERROR_END;
		return false;
	}

	std::unordered_set<STRING> ok;
	for (UINT i = 0; i < subs->size(); i += 2)
	{
		const STRING& id = (*subs)[i];
		const STRING& name = (*subs)[i + 1];

		auto it = m_SubsMap.find(id);
		if (it == m_SubsMap.end())
		{
			auto it2 = m_UseDefault.find(id);
			if (it2 == m_UseDefault.end() || it2->second == name)
			{
				continue;
			}
			return false;
		}
		else
		{
			if (it->second == name)
			{
				ok.insert(id);
				continue;
			}
			return false;
		}
	}

	if (ok.size() == m_SubsList.size())
		return true;
	return false;
}
