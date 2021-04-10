#ifndef _YBEHAVIOR_CONTEXT_H_
#define _YBEHAVIOR_CONTEXT_H_

#include "YBehavior/types.h"
#include "YBehavior/utility.h"
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
	public:
		TransitionResult transferResult;
	public:
		TransitionContext();
		Transition& Get() { return m_Trans; }
		const Transition& Get() const { return m_Trans; }
		bool IncTransCount();
		inline void ResetTransCount() { m_TransCount = 0; }
		void Set(const STRING& e) { m_Trans.TrySet(e); }
		void UnSet(const STRING& e) { m_Trans.UnSet(e); }
		inline void Reset()
		{
			m_Trans.Reset(); ResetTransCount();
		}
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
	public:
		void PushCallStack(TreeNodeContext* context) { m_CallStack.push(context); }
		TreeNodeContext* GetCallStackTop() { if (m_CallStack.empty()) return nullptr; return m_CallStack.top(); }
		void PopCallStack() { if (!m_CallStack.empty()) m_CallStack.pop(); }
		bool IsCallStackEmpty() { return m_CallStack.empty(); }
	};
}

#endif