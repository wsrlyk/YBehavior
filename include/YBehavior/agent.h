#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include "tools/linkedlist.h"
namespace YBehavior
{
	class BehaviorTree;
	class SharedDataEx;
	class RegisterData;

	class YBEHAVIOR_API Entity
	{
	protected:
		LinkedList<EntityWrapper>* m_WrapperList;
	public:
		Entity();
		~Entity();
		EntityWrapper CreateWrapper();
		void DeleteWrapper(LinkedListNode<EntityWrapper>* node);
		virtual STRING ToString() const;
	};

	class YBEHAVIOR_API Agent
	{
		static UINT s_UID;
		Uint m_UID;

		SharedDataEx* m_SharedData;
		BehaviorTree* m_Tree;
		RegisterData* m_RegisterData;
		Entity* m_Entity;
	public:
		Agent();
		~Agent();
		inline SharedDataEx* GetSharedData() { return m_SharedData; }
		inline BehaviorTree* GetTree() { return m_Tree; }
		inline Entity* GetEntity() { return m_Entity; }
		inline void SetEntity(Entity* entity) { m_Entity = entity; }
		RegisterData* GetRegister();
		void SetTree(const STRING& name);
		void Tick();

		void ProcessRegister();
		inline UINT GetUID() { return m_UID; }
	protected:
		virtual void _OnProcessRegister() {}
	};
}

#endif