#ifndef _YBEHAVIOR_BEHAVIORMGR_H_
#define _YBEHAVIOR_BEHAVIORMGR_H_

#include "YBehavior/types.h"
#include "YBehavior/singleton.h"
#include <unordered_map>
#include "behavior.h"
#include "YBehavior/version.h"

///>///////////////////////////////////////////////////////////////////////
///> Machine->MacTreeMap->Tree
///> We wont support SubMachineNode like SubTreeNode in Tree, so all info about a machine is in a single
///> file, with external params. And the mapping beween machines and trees is put in a separate class,
///> thus when trees change or mappings change, the machine which is focused on the relations 
///> of the states, will keep unchanged.

namespace YBehavior
{
	typedef VersionMgr<Behavior, UINT> BehaviorVersionMgrType;
	typedef typename BehaviorVersionMgrType::InfoType BehaviorInfoType;
	typedef typename BehaviorVersionMgrType::InfoType::VersionType BehaviorVersionType;

	struct BehaviorKey;
	class BehaviorMgr
	{
		BehaviorVersionMgrType m_VersionMgr;

	public:
		Behavior* GetBehavior(const BehaviorKey& key);
		void ReturnBehavior(Behavior* pBehavior);

		void ReloadTree(const STRING& name);
		void ReloadMachine(const STRING& name);
		void ReloadAll();
		void Print();
	};
}

#endif