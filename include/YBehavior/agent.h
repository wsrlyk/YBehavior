#ifndef _YBEHAVIOR_AGENT_H_
#define _YBEHAVIOR_AGENT_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
namespace YBehavior
{
	class BehaviorTree;
	class SharedDataEx;
	class RegisterData;
	class YBEHAVIOR_API Agent
	{
		static UINT s_UID;
		SharedDataEx* m_SharedData;
		BehaviorTree* m_Tree;
		RegisterData* m_RegisterData;
		Uint m_UID;

	public:
		Agent();
		~Agent();
		inline SharedDataEx* GetSharedData() { return m_SharedData; }
		inline BehaviorTree* GetTree() { return m_Tree; }
		RegisterData* GetRegister();
		void SetTree(const STRING& name);
		void Tick();
		inline UINT GetUID() { return m_UID; }

		void ProcessRegister();
	protected:
		virtual void _OnProcessRegister();
	};
}

#endif