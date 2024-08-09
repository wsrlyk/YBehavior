#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "YBehavior/treenode.h"
#include "YBehavior/types/treemap.h"
#include "YBehavior/memory.h"
#include "YBehavior/types/smallmap.h"
#include <unordered_map>
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
		LocalMemoryInOut(AgentPtr pAgent, std::vector<IPin* >* pInputsFrom, std::vector<IPin* >* pOutputsTo);
		void Set(AgentPtr pAgent, std::vector<IPin* >* pInputsFrom, std::vector<IPin* >* pOutputsTo);
		void OnInput(StdVector<IPin*>* pInputsTo);
		void OnOutput(StdVector<IPin*>* pOutputsFrom);
	private:
		AgentPtr m_pAgent{};
		std::vector<IPin* >* m_pInputsFrom{};
		std::vector<IPin* >* m_pOutputsTo{};
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
		inline void SetVersion(void* v) { m_Version = v; }
		inline void* GetVersion() const { return m_Version; }
#ifdef YDEBUGGER
		inline UINT GetHash() { return m_Hash; }
		inline void SetHash(UINT hash) { m_Hash = hash; }
#endif
	private:
		VariableCollection* m_SharedData;	///> Original data, copied to each agent using this tree
		VariableCollection* m_LocalData;	///> Original local data, pushed to the memory of an agent once run this tree
		ObjectPool<VariableCollection> m_LocalDataPool;
		//NameKeyMgr* m_NameKeyMgr;
		STRING m_TreeNameWithPath;	///> Full Path
		STRING m_TreeName;	///> Only File
		void* m_Version;
		TreeMap m_TreeMap;

		small_map<UINT, UINT> m_ValidEvents;
		std::unordered_map<STRING, UINT> m_TreeNodeCounts;
#ifdef YDEBUGGER
		UINT m_Hash;
#endif

		StdVector<IPin*> m_Inputs;
		StdVector<IPin*> m_Outputs;
	public:
		BehaviorTree(const STRING& name);
		~BehaviorTree();
		inline const STRING& GetKey() const { return m_TreeNameWithPath; }
		inline const STRING& GetFullName() const { return m_TreeNameWithPath; }
		inline const STRING& GetTreeName() const { return m_TreeName; }
		inline const VariableCollection* GetSharedData() const { return m_SharedData; }
		inline const auto& GetValidEvents() const { return m_ValidEvents; }
		inline const auto& GetTreeNodeCounts() const { return m_TreeNodeCounts; }
		VariableCollection* GetLocalData();
		inline VariableCollection* GetLocalDataIfExists() { return m_LocalData; }
		inline ObjectPool<VariableCollection>& GetLocalDataPool() { return m_LocalDataPool; }

		inline TreeMap& GetTreeMap() { return m_TreeMap; }
		//inline NameKeyMgr* GetNameKeyMgr() { return m_NameKeyMgr; }

		void RegiseterEvent(UINT e, UINT count);
		void AddTreeNodeCount(const STRING& name);

		TreeNodeContext* CreateRootContext(LocalMemoryInOut* pTunnel = nullptr);

		bool ProcessDataConnections(const std::vector<TreeNode*>& treeNodeCache, const pugi::xml_node& data);
	protected:
		bool OnLoadChild(const pugi::xml_node& data) override;
	};

}

#endif