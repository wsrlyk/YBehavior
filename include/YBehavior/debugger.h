#ifndef _YBEHAVIOR_DEBUGGER_H_
#define _YBEHAVIOR_DEBUGGER_H_


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

	struct DebugPointInfo
	{
		UINT nodeUID;
		
		INT count = 0;

		inline bool HasBreakPoint() { return count > 0; }
		inline bool HasLogPoint() { return count < 0; }
		inline bool NoDebugPoint() { return count == 0; }
	};
	class DebugMgr: public Singleton<DebugMgr>
	{
		UINT m_TargetAgent;
		STRING m_TargetTree;
		bool m_bTargetDirty = false;

		std::list<NodeRunInfo*> m_RunInfos;
		std::unordered_map<UINT, DebugPointInfo> m_DebugPointInfos;

		STRING m_SendBuffer;

		bool m_bPaused = false;
	public:
		void SetTarget(const STRING& tree, UINT agent);
		void ResetTarget();
		void Stop();
		bool IsValidTarget(Agent* pAgent);
		inline UINT GetTargetAgent() { return m_TargetAgent; }
		inline const std::list<NodeRunInfo*>& GetRunInfos() { return m_RunInfos; }
		bool TryHitBreakPoint(UINT nodeUID);
		void ClearDebugPoints() { m_DebugPointInfos.clear(); }
		void AddBreakPoint(UINT nodeUID);
		void AddLogPoint(UINT nodeUID);
		void RemoveDebugPoint(UINT nodeUID);
		NodeRunInfo* CreateAndAppendRunInfo();
		void Clear();

		inline void TogglePause(bool bPaused) { m_bPaused = bPaused; }
		inline bool IsPaused() { return m_bPaused; }

		void AppendSendContent(const STRING& s) { m_SendBuffer += s; }
		void Send(bool bClearRunInfo);
	};

	class Agent;
	class DebugHelper
	{
		Agent* m_Target;
		BehaviorNode* m_pNode;
		NodeRunInfo* m_pRunInfo;

		void _SendInfos(const STRING& treeName);
		void _SendCurrentInfos();
		void _SendPause();
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
