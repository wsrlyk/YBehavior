#ifndef _YBEHAVIOR_DEBUGGER_H_
#define _YBEHAVIOR_DEBUGGER_H_

#ifdef DEBUGGER
#include "YBehavior/define.h"
#include "YBehavior/singleton.h"
#include "YBehavior/behaviortree.h"

#include <list>
#include <unordered_map>

namespace YBehavior
{
	struct NodeRunInfo
	{
		UINT nodeUID;
		NodeState runState;

		const STRING ToString() const;
	};
	struct SharedVariableInfo
	{
		STRING name;
		STRING value;
	};

	struct BreakPointInfo
	{
		UINT nodeUID;
		// TODO
		// counter, etc.
	};
	class DebugMgr: public Singleton<DebugMgr>
	{
		UINT m_TargetAgent;
		STRING m_TargetTree;
		bool m_bTargetDirty = false;

		std::list<NodeRunInfo*> m_RunInfos;
		std::unordered_map<UINT, BreakPointInfo> m_BreakPointInfos;

		STRING m_SendBuffer;

		bool m_bPaused = true;
	public:
		void SetTarget(const STRING& tree, UINT agent);
		bool IsValidTarget(Agent* pAgent);
		inline UINT GetTargetAgent() { return m_TargetAgent; }
		inline const std::list<NodeRunInfo*>& GetRunInfos() { return m_RunInfos; }
		bool TryHitBreakPoint(UINT nodeUID);
		void ClearBreakPoints() { m_BreakPointInfos.clear(); }
		void AddBreakPoint(UINT nodeUID);

		NodeRunInfo* CreateAndAppendRunInfo();
		void Clear();

		inline void TogglePause(bool bPaused) { m_bPaused = bPaused; }
		inline bool IsPaused() { return m_bPaused; }

		void AppendSendContent(const STRING& s) { m_SendBuffer += s; }
		void Send();
	};

	class Agent;
	class DebugHelper
	{
		Agent* m_Target;
		BehaviorNode* m_pNode;
		NodeRunInfo* m_pRunInfo;

		void _SendInfos(const STRING& treeName);
		void _SendCurrentInfos();
	public:
		DebugHelper(Agent* pAgent, BehaviorNode* pNode);
		~DebugHelper();
		inline bool IsValid() { return m_Target != nullptr; }
		void CreateRunInfo();
		void SetResult(NodeState state);
		void TryHitBreakPoint();

		void Breaking();
	};
}
#endif

#endif