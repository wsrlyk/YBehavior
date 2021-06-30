#ifndef _YBEHAVIOR_DEBUGGER_H_
#define _YBEHAVIOR_DEBUGGER_H_

#ifdef YDEBUGGER

#include "YBehavior/define.h"
#include "YBehavior/singleton.h"
#include "YBehavior/behaviortree.h"

#include <list>
#include <unordered_map>
#include "YBehavior/network/network.h"
#include "YBehavior/fsm/statemachine.h"

namespace YBehavior
{
	enum struct DebugTargetType;
	
	struct NodeRunInfo
	{
		UINT nodeUID;
		int rawRunState;
		int finalRunState;
		const void* pRootNode;
		const void* pNode;
		DebugTargetType type;
		const STRING ToString() const;
	};
	struct NodeLogInfo
	{
		UINT nodeUID;
		StdVector<STRING> beforeInfo;
		StdVector<STRING> afterInfo;
		STRING otherInfo;
		NodeState state;

		void Reset()
		{
			nodeUID = 0;
			beforeInfo.clear();
			afterInfo.clear();
			otherInfo = Utility::StringEmpty;
			state = NS_INVALID;
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

	struct GraphDebugInfo
	{
		std::unordered_map<UINT, DebugPointInfo> DebugPointInfos;
		UINT Hash;

		void Reset()
		{
			Hash = 0;
			DebugPointInfos.clear();
		}
	};

	enum DebugCommand
	{
		DC_None,
		DC_Continue,
		DC_StepInto,
		DC_StepOver,
	};
	enum struct DebugTargetType
	{
		INVALID = -1,
		TREE,
		FSM,
	};
	struct DebugTargetID
	{
		DebugTargetID()
			: Type(DebugTargetType::INVALID)
			, Name(Utility::StringEmpty)
		{}
		DebugTargetID(DebugTargetType type, const STRING& name)
			: Type(type)
			, Name(name)
		{}
		DebugTargetType Type;
		STRING Name;
		bool operator==(const DebugTargetID& other) const
		{
			return Type == other.Type && Name == other.Name;
		}
		class Hash
		{
		public:
			size_t operator()(const DebugTargetID& o) const
			{
				return Utility::hash_val(o.Name, o.Type);
			}
		};
	};

	class IDebugHelper;
	class DebugMgr: public Singleton<DebugMgr>
	{
		UINT64 m_TargetAgent = 0;
		UINT64 m_TryTarget = 0;
		//UINT m_TargetHash;
		DebugTargetID m_Target;
		bool m_bTargetDirty = false;

		Mutex m_Mutex;
		///> Node(Tree or FSM) => RunInfo, one node has only one info
		std::unordered_map<const void*, NodeRunInfo*> m_RunInfos;
		//std::unordered_map<UINT, DebugPointInfo> m_DebugPointInfos;

		std::unordered_map<STRING, GraphDebugInfo> m_TreeDebugInfo;
		GraphDebugInfo m_FSMDebugInfo;

		STRING m_SendBuffer;

		bool m_bPaused = false;

		DebugCommand m_Command = DC_None;
		IDebugHelper* m_StepOverHelper = nullptr;
	public:
		~DebugMgr();
		void SetTarget(const DebugTargetID& target);
		void SetTarget(UINT64 target);
		void Begin();
		void ResetTarget();
		void Stop();
		bool IsValidTarget(Agent* pAgent, BehaviorTree* pTree);
		bool IsValidTarget(Agent* pAgent, FSM* pFSM);
		inline const DebugTargetID& GetTarget() { return m_Target; }
		inline UINT64 GetTargetAgent() { return m_TargetAgent; }

		inline Mutex& GetMutex() { return m_Mutex; }
		bool HasBreakPoint(const DebugTargetID& target, UINT nodeUID);
		bool HasLogPoint(const DebugTargetID& target, UINT nodeUID);
		bool HasDebugPoint(const DebugTargetID& target, UINT nodeUID);
		void ClearDebugInfos() { m_TreeDebugInfo.clear(); m_FSMDebugInfo.Reset(); }
		void AddBreakPoint(const DebugTargetID& target, UINT nodeUID);
		void AddLogPoint(const DebugTargetID& target, UINT nodeUID);
		void AddTreeDebugInfo(const DebugTargetID& target, GraphDebugInfo&& info);
		void RemoveDebugPoint(const DebugTargetID& target, UINT nodeUID);
		GraphDebugInfo* FindGraphDebugInfo(const DebugTargetID& target);
		NodeRunInfo* CreateAndAppendRunInfo(const void* pNode);
		void Clear();

		inline void TogglePause(bool bPaused) { m_bPaused = bPaused; }
		inline bool IsPaused() { return m_bPaused; }

		void AppendSendContent(const STRING& s) { m_SendBuffer += s; }
		void AppendSendContent(const char c) { m_SendBuffer += c; }
		void Send(bool bClearRunInfo);

		void SendInfos(AgentPtr pTarget, bool clear);
		inline void SetCommand(DebugCommand cmd) { m_Command = cmd; }
		inline DebugCommand GetCommand() const { return m_Command; }
		inline void SetStepOverHelper(IDebugHelper* helper) { m_StepOverHelper = helper; }
		inline IDebugHelper* GetStepOverHelper() const { return m_StepOverHelper; }
	private:
		void _TryDebug(AgentPtr pAgent);
	};

	class IDebugHelper
	{
	protected:
		static unsigned s_Token;

		NodeRunInfo* m_pRunInfo{};
		unsigned m_Token;
		Agent* m_Target{};
		DebugTargetType m_Type;
	public:
		IDebugHelper() {};
		IDebugHelper(Agent* pAgent) : m_Target(pAgent){}
		virtual ~IDebugHelper() {}
		inline bool IsValid() { return m_Target != nullptr; }
		void CreateRunInfo(const void* pNode);
		void SetResult(int rawState, int finalState);
		void TryBreaking();
		void TryPause();
		bool HasLogPoint();
		bool HasDebugPoint();
		virtual const STRING& GetRootName() = 0;
	protected:
		void _SendPause();
		void _AssignToken() { m_Token = ++s_Token; }
		void _SetBreak();
		void _Breaking();
		void _SendCurrentInfos();
	public:
		static const char s_HeadSpliter = (char)3;
		static const char s_ContentSpliter = (char)4;
		//static const char s_SectionSpliter = (char)5;
		static const char s_SequenceSpliter = (char)6;
		static const char s_ListSpliter = (char)7;
	};

	class Agent;
	class TreeNodeContext;

	///> For now, tree node is different from machine state
	///  TreeNodeContext is managed by pool, and each TreeNodeContext will keep a DebugTreeHelper,
	///  So it means a DebugTreeHelper may never be deleted until the app ends.
	///  As a result, m_DebugLogInfo can be managed by the DebugTreeHelper 
	///  and you don't need worrying about allocating a large stringstream for storing the logs
	class DebugTreeHelper : public IDebugHelper
	{
		TreeNodeContext* m_pContext;
		NodeLogInfo* m_pLogInfo;
		std::stringstream m_DebugLogInfo;

	public:
		DebugTreeHelper(TreeNodeContext* pContext) : m_pContext(pContext) {}
		void Init(Agent* pAgent);
		void Dispose();
		const STRING& GetRootName() override;
		void TryCreateRunInfo();
		void SendLogPoint();
	public:
		void LogSharedData(ISharedVariableEx* pVariable, bool bBefore);
		inline std::stringstream& GetDebugLogInfo() { return m_DebugLogInfo; }
	private:
		void _CreateTreeRunInfo();
	};

	class DebugFSMHelper : public IDebugHelper
	{
		MachineState* m_pNode;
		NodeLogInfo* m_pLogInfo;

		void _SendLogPoint();
	public:
		DebugFSMHelper(Agent* pAgent, MachineState* pNode);
		~DebugFSMHelper();
		const STRING& GetRootName() override;

	};

}

#endif // YDEBUGGER

#endif
