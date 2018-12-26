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
		BehaviorTree* tree;

		const STRING ToString() const;
	};
	struct NodeLogInfo
	{
		UINT nodeUID;
		StdVector<STRING> beforeInfo;
		StdVector<STRING> afterInfo;
		STRING otherInfo;

		void Reset()
		{
			nodeUID = 0;
			beforeInfo.clear();
			afterInfo.clear();
			otherInfo = Utility::StringEmpty;
		}
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

	struct TreeDebugInfo
	{
		std::unordered_map<UINT, DebugPointInfo> DebugPointInfos;
		UINT Hash;
	};

	enum DebugCommand
	{
		DC_None,
		DC_Continue,
		DC_StepInto,
		DC_StepOver,
	};

	class DebugHelper;
	class DebugMgr: public Singleton<DebugMgr>
	{
		UINT64 m_TargetAgent;
		//UINT m_TargetHash;
		STRING m_TargetTree;
		bool m_bTargetDirty = false;

		std::list<NodeRunInfo*> m_RunInfos;
		//std::unordered_map<UINT, DebugPointInfo> m_DebugPointInfos;

		std::unordered_map<STRING, TreeDebugInfo> m_TreeDebugInfo;

		STRING m_SendBuffer;

		bool m_bPaused = false;

		DebugCommand m_Command = DC_None;
		DebugHelper* m_StepOverHelper = nullptr;
	public:
		~DebugMgr();
		void SetTarget(const STRING& tree, UINT agent);
		void ResetTarget();
		void Stop();
		bool IsValidTarget(Agent* pAgent, BehaviorTree* pTree);
		inline const STRING& GetTargetTree() { return m_TargetTree; }
		inline UINT64 GetTargetAgent() { return m_TargetAgent; }
		inline const std::list<NodeRunInfo*>& GetRunInfos() { return m_RunInfos; }
		bool HasBreakPoint(const STRING& treeName, UINT nodeUID);
		bool HasLogPoint(const STRING& treeName, UINT nodeUID);
		bool HasDebugPoint(const STRING& treeName, UINT nodeUID);
		void ClearTreeDebugInfo() { m_TreeDebugInfo.clear(); }
		void AddBreakPoint(const STRING& treeName, UINT nodeUID);
		void AddLogPoint(const STRING& treeName, UINT nodeUID);
		void AddTreeDebugInfo(STRING&& name, TreeDebugInfo&& info);
		void RemoveDebugPoint(const STRING& treeName, UINT nodeUID);
		NodeRunInfo* CreateAndAppendRunInfo();
		void Clear();

		inline void TogglePause(bool bPaused) { m_bPaused = bPaused; }
		inline bool IsPaused() { return m_bPaused; }

		void AppendSendContent(const STRING& s) { m_SendBuffer += s; }
		void AppendSendContent(const char c) { m_SendBuffer += c; }
		void Send(bool bClearRunInfo);

		inline void SetCommand(DebugCommand cmd) { m_Command = cmd; }
		inline DebugCommand GetCommand() const { return m_Command; }
		inline void SetStepOverHelper(DebugHelper* helper) { m_StepOverHelper = helper; }
		inline DebugHelper* GetStepOverHelper() const { return m_StepOverHelper; }
	};

	class Agent;
	class DebugHelper
	{
		static unsigned s_Token;

		unsigned m_Token;
		Agent* m_Target;
		BehaviorNode* m_pNode;
		NodeRunInfo* m_pRunInfo;
		NodeLogInfo* m_pLogInfo;

		void _SendInfos(bool clear);
		void _SendCurrentInfos();
		void _SendPause();
		void _SendLogPoint();
	public:
		DebugHelper(Agent* pAgent, BehaviorNode* pNode);
		~DebugHelper();
		inline bool IsValid() { return m_Target != nullptr; }
		void CreateRunInfo();
		void SetResult(NodeState state);
		void TestBreaking();
		void TestPause();
		bool HasLogPoint();
		bool HasDebugPoint();
		void Breaking();
		void SetBreak();
	public:
		void LogSharedData(ISharedVariableEx* pVariable, bool bBefore);
		
		static const char s_HeadSpliter = (char)3;
		static const char s_ContentSpliter = (char)4;
		static const char s_SectionSpliter = (char)5;
		static const char s_SequenceSpliter = (char)6;
		static const char s_ListSpliter = (char)7;
	};
}

#endif // DEBUGGER

#endif
