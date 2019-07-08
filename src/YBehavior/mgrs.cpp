#include "YBehavior/mgrs.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/fsm/machinetreemappingmgr.h"

namespace YBehavior
{
	Mgrs::Mgrs()
	{
		m_pTreeMgr = new TreeMgr();
		m_pMachineMgr = new MachineMgr();
		m_pMappingMgr = new MachineTreeMappingMgr();
	}

	Mgrs::~Mgrs()
	{
		delete m_pMappingMgr;
		delete m_pMachineMgr;
		delete m_pTreeMgr;
	}

}
