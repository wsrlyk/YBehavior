#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"

namespace YBehavior
{
	class BehaviorTree;
	class SharedDataEx;
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
		static UINT64 s_UID;
		UINT64 m_UID;
		BehaviorProcess* m_pProcess;
		Entity* m_Entity;

	public:
		Agent(Entity* entity);
		virtual ~Agent();
		inline Entity* GetEntity() { return m_Entity; }
		inline void SetEntity(Entity* entity) { m_Entity = entity; }

		Behavior* GetBehavior();
		Memory* GetMemory();
		MachineContext* GetMachineContext();
		TreeContext* GetTreeContext();
		BehaviorTree* GetRunningTree();
		EventQueue* GetEventQueue();

		bool SetBehavior(const BehaviorKey& key);
		void UnloadBehavior();
		void Tick();

		inline UINT64 GetUID() { return m_UID; }
		virtual UINT64 GetDebugUID() { return GetUID(); }

	};
}

#endif