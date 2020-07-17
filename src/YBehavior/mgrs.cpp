#include "YBehavior/mgrs.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/fsm/behaviormgr.h"

namespace YBehavior
{
	Mgrs::Mgrs()
	{
		m_pTreeMgr = new TreeMgr();
		m_pMachineMgr = new MachineMgr();
		m_pBehaviorMgr = new BehaviorMgr();
	}

	Mgrs::~Mgrs()
	{
		delete m_pBehaviorMgr;
		delete m_pMachineMgr;
		delete m_pTreeMgr;
	}

}
