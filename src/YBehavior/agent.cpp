#include "YBehavior/agent.h"
#include "YBehavior/behaviorprocess.h"
#ifdef YPROFILER
#include "YBehavior/profile/profilehelper.h"
#endif

namespace YBehavior
{
	UINT64 Agent::s_UID = 0;

	Behavior* Agent::GetBehavior() { return m_pProcess->pBehavior; }
	Memory* Agent::GetMemory() { return &m_pProcess->memory; }
	MachineContext* Agent::GetMachineContext() { return &m_pProcess->machineContext; }
	TreeContext* Agent::GetTreeContext() { return &m_pProcess->treeContext; }
	BehaviorTree* Agent::GetRunningTree() { return m_pProcess->machineContext.GetCurRunningTree(); }
	EventQueue* Agent::GetEventQueue() { return &m_pProcess->eventQueue; }

	bool Agent::SetBehavior(const BehaviorKey& key)
	{
		UnloadBehavior();

		if (!BehaviorProcessHelper::GetBehaviorProcess(key, *m_pProcess))
			return false;

		return true;
	}


	void Agent::UnloadBehavior()
	{
		BehaviorProcessHelper::Release(*m_pProcess);
	}

	void Agent::Tick()
	{
#ifdef YPROFILER
		Profiler::AgentProfileHelper helper(this);
#endif
		BehaviorProcessHelper::Execute(this);
	}

	Agent::Agent(Entity* entity)
		: m_Entity(entity)
	{
		m_UID = ++s_UID;
		m_pProcess = new BehaviorProcess();
	}

	Agent::~Agent()
	{
		UnloadBehavior();
		delete m_pProcess;
	}

	Entity::Entity()
	{
		m_Wrapper = nullptr;
	}

	Entity::~Entity()
	{
		if (m_Wrapper)
		{
			m_Wrapper->SetValid(false);
			delete m_Wrapper;
		}
	}

	STRING Entity::ToString() const
	{
		return Utility::StringEmpty;
	}

	const EntityWrapper& Entity::GetWrapper()
	{
		if (m_Wrapper == nullptr)
			m_Wrapper = new EntityWrapper(this);

		return *m_Wrapper;
	}
}