#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <stack>

namespace YBehavior
{
	class BehaviorTree;
	class SharedDataEx;
	class RegisterData;
	class RunningContext;
	class Memory;

	class YBEHAVIOR_API Entity
	{
	protected:
		EntityWrapper* m_Wrapper;
	public:
		Entity();
		~Entity();
		const EntityWrapper& GetWrapper();
		virtual STRING ToString() const;
	};

	class YBEHAVIOR_API Agent
	{
		static UINT64 s_UID;
		UINT64 m_UID;

		Memory* m_Memory;
		//SharedDataEx* m_SharedData;
		BehaviorTree* m_Tree;
		RegisterData* m_RegisterData;
		Entity* m_Entity;

		std::stack<RunningContext*> m_RunningContexts;
	public:
		Agent(Entity* entity);
		~Agent();
		inline Memory* GetMemory() { return m_Memory; }
		//inline SharedDataEx* GetSharedData() { return m_SharedData; }
		inline BehaviorTree* GetTree() { return m_Tree; }
		inline Entity* GetEntity() { return m_Entity; }
		inline void SetEntity(Entity* entity) { m_Entity = entity; }
		RegisterData* GetRegister();
		bool SetTree(const STRING& name, const std::vector<STRING>* subs = nullptr);
		void UnloadTree();
		void Tick();

		void ProcessRegister();
		inline UINT64 GetUID() { return m_UID; }
		virtual UINT64 GetDebugUID() { return GetUID(); }
		RunningContext* PopRC();
		void PushRC(RunningContext* context);
		void ClearRC();
	protected:
		virtual void _OnProcessRegister() {}
	};
}

#endif