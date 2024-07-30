#ifndef _YBEHAVIOR_MGRS_H_
#define _YBEHAVIOR_MGRS_H_

#include "singleton.h"
#include "define.h"

namespace YBehavior
{
	class TreeMgr;
	class MachineMgr;
	class BehaviorMgr;
	class Mgrs : public Singleton<Mgrs>
	{
	public:
		Mgrs();
		~Mgrs();
		void Reset();
		inline TreeMgr* GetTreeMgr() { return m_pTreeMgr; }
		inline MachineMgr* GetMachineMgr() { return m_pMachineMgr; }
		inline BehaviorMgr* GetBehaviorMgr() { return m_pBehaviorMgr; }

	protected:
		TreeMgr* m_pTreeMgr;
		MachineMgr* m_pMachineMgr;
		BehaviorMgr* m_pBehaviorMgr;
	};

}

#endif