#ifndef _YBEHAVIOR_CONTEXT_H_
#define _YBEHAVIOR_CONTEXT_H_

#include "YBehavior/types/types.h"
#include <list>
#include "transition.h"
#include "machinestate.h"
#include <stack>

namespace YBehavior
{
	class TransitionContext
	{
		Transition m_Trans;
		int m_TransCount{ 0 };
		bool m_IsNeedCheck{ true };
	public:
		TransitionResult transferResult;
	public:
		TransitionContext();
		Transition& Get() { return m_Trans; }
		const Transition& Get() const { return m_Trans; }
		bool IncTransCount();
		bool IsNeedCheck() const { return m_IsNeedCheck; }
		inline void ResetTransCount() { m_TransCount = 0; }
		void Set(const STRING& e) { m_Trans.TrySet(e); m_IsNeedCheck = true; }
		void UnSet(const STRING& e) { m_Trans.UnSet(e); m_IsNeedCheck = true; }
		inline void Reset()
		{
			m_Trans.Reset(); ResetTransCount(); m_IsNeedCheck = true;
		}
		void NoNeedCheck() { m_IsNeedCheck = false; }
	};

	//typedef std::list<MachineState*> CurrentStatesType;
	class Behavior;
	class BehaviorTree;
	class MachineContext
	{
	protected:
		//CurrentStatesType m_CurStates;
		MachineState* m_pCurState;
		TransitionContext m_Trans;
		BehaviorTree* m_pCurRunningTree;

		std::list<TransQueueData> m_pTransQueue;
	public:
		MachineRunRes LastRunRes;

	public:
		MachineContext();
		inline TransitionContext& GetTransition() { return m_Trans; }
		inline const TransitionContext& GetTransition() const { return m_Trans; }
		//inline CurrentStatesType& GetCurStatesStack() { return m_CurStates; }
		void Init(Behavior* behavior);
		inline void ResetCurRunning() { m_pCurRunningTree = nullptr; }
		inline void SetCurRunning(BehaviorTree* pCurRunningTree) { m_pCurRunningTree = pCurRunningTree; }
		inline BehaviorTree* GetCurRunningTree() { return m_pCurRunningTree; }
		inline void SetCurState(MachineState* pState) { m_pCurState = pState; }
		inline MachineState* GetCurState() const { return m_pCurState; }

		inline std::list<TransQueueData>& GetTransQueue() { return m_pTransQueue; }

		void Reset();
	};

	class TreeNodeContext;
	class TreeContext
	{
	protected:
		std::stack<TreeNodeContext*> m_CallStack;
		bool m_bDirty{ false };
	public:
		inline void PushCallStack(TreeNodeContext* context) { m_CallStack.push(context); m_bDirty = true; }
		inline TreeNodeContext* GetCallStackTop() { m_bDirty = false; return m_CallStack.top(); }
		inline void PopCallStack() { m_CallStack.pop(); }
		inline bool IsCallStackEmpty() const { return m_CallStack.empty(); }
		inline bool HasNewCall() const { return m_bDirty; }
		void Reset();
	};
}

#endif