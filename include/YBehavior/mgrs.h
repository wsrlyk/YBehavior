#ifndef _YBEHAVIOR_MGRS_H_
#define _YBEHAVIOR_MGRS_H_

#include "singleton.h"

namespace YBehavior
{
	class TreeMgr;
	class MachineMgr;
	class MachineTreeMappingMgr;
	class Mgrs : public Singleton<Mgrs>
	{
	public:
		Mgrs();
		~Mgrs();

		inline TreeMgr* GetTreeMgr() { return m_pTreeMgr; }
		inline MachineMgr* GetMachineMgr() { return m_pMachineMgr; }
		inline MachineTreeMappingMgr* GetMappingMgr() { return m_pMappingMgr; }

	protected:
		TreeMgr* m_pTreeMgr;
		MachineMgr* m_pMachineMgr;
		MachineTreeMappingMgr* m_pMappingMgr;
	};

}

#endif