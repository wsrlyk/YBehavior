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
	struct NodeLogInfo
	{
		UINT nodeUID;
		std::vector<STRING> beforeInfo;
		std::vector<STRING> afterInfo;
		STRING otherInfo;
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
		UINT m_TargetHash;
		STRING m_TargetTree;
		bool m_bTargetDirty = false;

		std::list<NodeRunInfo*> m_RunInfos;
		std::unordered_map<UINT, DebugPointInfo> m_DebugPointInfos;

		STRING m_SendBuffer;

		bool m_bPaused = false;
	public:
		~DebugMgr();
		void SetTarget(const STRING& tree, UINT hash, UINT agent);
		void ResetTarget();
		void Stop();
		bool IsValidTarget(Agent* pAgent);
		inline const STRING& GetTargetTree() { return m_TargetTree; }
		inline UINT GetTargetAgent() { return m_TargetAgent; }
		inline const std::list<NodeRunInfo*>& GetRunInfos() { return m_RunInfos; }
		bool HasBreakPoint(UINT nodeUID);
		bool HasLogPoint(UINT nodeUID);
		void ClearDebugPoints() { m_DebugPointInfos.clear(); }
		void AddBreakPoint(UINT nodeUID);
		void AddLogPoint(UINT nodeUID);
		void RemoveDebugPoint(UINT nodeUID);
		NodeRunInfo* CreateAndAppendRunInfo();
		void Clear();

		inline void TogglePause(bool bPaused) { m_bPaused = bPaused; }
		inline bool IsPaused() { return m_bPaused; }

		void AppendSendContent(const STRING& s) { m_SendBuffer += s; }
		void AppendSendContent(const char c) { m_SendBuffer += c; }
		void Send(bool bClearRunInfo);
	};

	class Agent;
	class DebugHelper
	{
		Agent* m_Target;
		BehaviorNode* m_pNode;
		NodeRunInfo* m_pRunInfo;
		NodeLogInfo* m_pLogInfo;

		void _SendInfos();
		void _SendCurrentInfos();
		void _SendPause();
		void _SendLogPoint();
	public:
		DebugHelper(Agent* pAgent, BehaviorNode* pNode);
		~DebugHelper();
		inline bool IsValid() { return m_Target != nullptr; }
		void CreateRunInfo();
		void SetResult(NodeState state);
		void TryHitBreakPoint();
		bool HasLogPoint();
		void Breaking();

	public:
		void LogSharedData(ISharedVariableEx* pVariable, bool bBefore);
		
		static const char s_HeadSpliter = (char)3;
		static const char s_ContentSpliter = (char)4;
	};
}

#endif // DEBUGGER

#endif
