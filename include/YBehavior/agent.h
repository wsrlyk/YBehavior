#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/types/types.h"
#include "behaviorprocess.h"

namespace YBehavior
{
	class BehaviorTree;
	class SharedDataEx;
	class RegisterData;
	class Memory;
	class MachineContext;

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

		//Memory* m_Memory;
		//MachineContext* m_pMachineContext;
		BehaviorProcess m_Process;

		//BehaviorTree* m_Tree;
		RegisterData* m_RegisterData;
		Entity* m_Entity;

	public:
		Agent(Entity* entity);
		virtual ~Agent();
		inline Behavior* GetBehavior() { return m_Process.pBehavior; }
		inline Memory* GetMemory() { return &m_Process.memory; }
		inline MachineContext* GetMachineContext() { return &m_Process.machineContext; }
		inline TreeContext* GetTreeContext() { return &m_Process.treeContext; }
		//inline SharedDataEx* GetSharedData() { return m_SharedData; }
		//inline BehaviorTree* GetTree() { return m_Tree; }
		inline BehaviorTree* GetRunningTree() { return m_Process.machineContext.GetCurRunningTree(); }
		inline Entity* GetEntity() { return m_Entity; }
		inline void SetEntity(Entity* entity) { m_Entity = entity; }
		RegisterData* GetRegister();
		//bool SetTree(const STRING& name, const std::vector<STRING>* subs = nullptr);
		bool SetBehavior(const BehaviorKey& key);
		//void UnloadTree();
		void UnloadBehavior();
		void Tick();

		void ProcessRegister();
		inline UINT64 GetUID() { return m_UID; }
		virtual UINT64 GetDebugUID() { return GetUID(); }

	protected:
		virtual void _OnProcessRegister() {}
	};
}

#endif