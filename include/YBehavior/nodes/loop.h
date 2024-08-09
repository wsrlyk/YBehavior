#ifndef _YBEHAVIOR_LOOP_H_
#define _YBEHAVIOR_LOOP_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	enum struct ForPhase
	{
		None,
		Init,
		Cond,
		Inc,
		Main,
	};

	class ForNodeContext : public CompositeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		int m_LoopTimes;
	};

	class For : public CompositeNode<ForNodeContext>
	{
		friend ForNodeContext;
	public:
		TREENODE_DEFINE(For)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		void OnAddChild(TreeNode * child, const STRING & connection) override;

		Pin<BOOL>* m_BreakValue = nullptr;
		TreeNodePtr m_InitChild = nullptr;
		TreeNodePtr m_CondChild = nullptr;
		TreeNodePtr m_IncChild = nullptr;
		TreeNodePtr m_MainChild = nullptr;
	};

	//////////////////////////////////////////////////////////////////////////
	class ForEachNodeContext : public SingleChildNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class ForEach : public SingleChildNode<ForEachNodeContext>
	{
		friend ForEachNodeContext;
	public:
		TREENODE_DEFINE(ForEach)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;

		Pin<BOOL>* m_BreakValue = nullptr;
		IPin* m_Collection = nullptr;
		IPin* m_Current = nullptr;
	};

	//////////////////////////////////////////////////////////////////////////

	class LoopNodeContext : public SingleChildNodeContext
	{
	protected:
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	};

	class Loop : public SingleChildNode<LoopNodeContext>
	{
		friend LoopNodeContext;
	public:
		TREENODE_DEFINE(Loop)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;

		Pin<BOOL>* m_BreakValue = nullptr;
		Pin<INT>* m_Count = nullptr;
		Pin<INT>* m_Current = nullptr;
	};

}

#endif
