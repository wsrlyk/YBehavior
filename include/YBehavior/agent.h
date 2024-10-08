#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"

namespace YBehavior
{
	class BehaviorTree;
	class VariableCollection;
	class EventQueue;
	class Memory;
	class MachineContext;
	class TreeContext;
	class Behavior;
	struct BehaviorProcess;
	struct BehaviorKey;
	class Entity
	{
	protected:
		EntityWrapper* m_Wrapper;
	public:
		Entity();
		virtual ~Entity();
		const EntityWrapper& GetWrapper();
		virtual STRING ToString() const;
	};

	class Agent
	{
	protected:
		static UINT64 s_UID;
		UINT64 m_UID;
		BehaviorProcess* m_pProcess;
		Entity* m_Entity;

	public:
		Agent(Entity* entity);
		virtual ~Agent();
		inline Entity* GetEntity() const { return m_Entity; }
		inline void SetEntity(Entity* entity) { m_Entity = entity; }

		Behavior* GetBehavior() const;
		Memory* GetMemory() const;
		MachineContext* GetMachineContext() const;
		TreeContext* GetTreeContext() const;
		BehaviorTree* GetRunningTree() const;
		EventQueue* GetEventQueue() const;

		bool SetBehavior(const BehaviorKey& key);
		void UnloadBehavior();
		void Tick();

		inline UINT64 GetUID() const { return m_UID; }
		virtual UINT64 GetDebugUID() const { return GetUID(); }

	};
}

#endif