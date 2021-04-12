#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "behaviornode.h"

//namespace pugi
//{
//	class xml_node;
//	class xml_attribute;
//}

namespace YBehavior
{
	class LocalMemoryInOut
	{
	public:
		LocalMemoryInOut() {};
		LocalMemoryInOut(AgentPtr pAgent, std::vector<ISharedVariableEx* >* pInputsFrom, std::vector<ISharedVariableEx* >* pOutputsTo);
		void Set(AgentPtr pAgent, std::vector<ISharedVariableEx* >* pInputsFrom, std::vector<ISharedVariableEx* >* pOutputsTo);
		void OnInput(std::unordered_map<STRING, ISharedVariableEx*>* pInputsTo);
		void OnOutput(std::unordered_map<STRING, ISharedVariableEx*>* pOutputsFrom);
	private:
		AgentPtr m_pAgent{};
		std::vector<ISharedVariableEx* >* m_pInputsFrom{};
		std::vector<ISharedVariableEx* >* m_pOutputsTo{};
		TempMemory m_TempMemory;
	};

	class BehaviorTreeContext : public SingleChildNodeContext
	{
		LocalMemoryInOut* m_pLocalMemoryInOut{};
	public:
		void Init(LocalMemoryInOut* pInOut) { m_pLocalMemoryInOut = pInOut; }
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class BehaviorTree : public SingleChildNode<BehaviorTreeContext>
	{
		friend BehaviorTreeContext;
	public:
		STRING GetClassName() const override { return "Tree"; }
		inline void SetVersion(void* v) { m_Version = v; }
		inline void* GetVersion() const { return m_Version; }
#ifdef YDEBUGGER
		inline UINT GetHash() { return m_Hash; }
		inline void SetHash(UINT hash) { m_Hash = hash; }
#endif
	private:
		SharedDataEx* m_SharedData;	///> Original data, copied to each agent using this tree
		SharedDataEx* m_LocalData;	///> Original local data, pushed to the memory of an agent once run this tree
		ObjectPool<SharedDataEx> m_LocalDataPool;
		//NameKeyMgr* m_NameKeyMgr;
		STRING m_TreeNameWithPath;	///> Full Path
		STRING m_TreeName;	///> Only File
		void* m_Version;
		TreeMap m_TreeMap;
#ifdef YDEBUGGER
		UINT m_Hash;
#endif

		StdVector<BehaviorTree*> m_SubTrees;
		std::unordered_map<STRING, ISharedVariableEx*> m_Inputs;
		std::unordered_map<STRING, ISharedVariableEx*> m_Outputs;
	public:
		BehaviorTree(const STRING& name);
		~BehaviorTree();
		inline const STRING& GetKey() const { return m_TreeNameWithPath; }
		inline const STRING& GetFullName() const { return m_TreeNameWithPath; }
		inline const STRING& GetTreeName() const { return m_TreeName; }
		inline SharedDataEx* GetSharedData() { return m_SharedData; }
		SharedDataEx* GetLocalData();
		inline SharedDataEx* GetLocalDataIfExists() { return m_LocalData; }
		inline ObjectPool<SharedDataEx>& GetLocalDataPool() { return m_LocalDataPool; }

		inline TreeMap& GetTreeMap() { return m_TreeMap; }
		//inline NameKeyMgr* GetNameKeyMgr() { return m_NameKeyMgr; }

		void MergeDataTo(SharedDataEx& destination);
		void AddSubTree(BehaviorTree* sub) { m_SubTrees.push_back(sub); }
		inline StdVector<BehaviorTree*>& GetSubTrees() { return m_SubTrees; }
		NodeState RootExecute(AgentPtr pAgent, NodeState parentState, LocalMemoryInOut* pTunnel = nullptr);

		TreeNodeContext* CreateRootContext(LocalMemoryInOut* pTunnel = nullptr);
		///> CAUTION: this function can only be called in garbage collection
		void ClearSubTree() { m_SubTrees.clear(); }
	protected:
		bool OnLoadChild(const pugi::xml_node& data) override;
	};

}

#endif