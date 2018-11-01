#ifndef _YBEHAVIOR_SUBTREE_H_
#define _YBEHAVIOR_SUBTREE_H_

#include "YBehavior/behaviortree.h"

namespace YBehavior
{
	class LocalMemoryCopier;
	class SubTree : public LeafNode
	{
		friend LocalMemoryCopier;
		typedef std::pair<ISharedVariableEx*, ISharedVariableEx*> FromToType;

	public:
		STRING GetClassName() const override { return "SubTree"; }
	protected:
		virtual NodeState Update(AgentPtr pAgent);
		virtual bool OnLoaded(const pugi::xml_node& data);
		bool OnLoadChild(const pugi::xml_node& data) override;
		bool _TryCreateFromTo(const pugi::xml_node& data, std::vector<FromToType >& container);
	private:
		SharedVariableEx<STRING>* m_TreeName;

		BehaviorTree* m_Tree = nullptr;

		std::vector<FromToType > m_Inputs;
		std::vector<FromToType > m_Outputs;
	};

	class LocalMemoryCopier : public ITreeExecutionHelper
	{
	public:
		LocalMemoryCopier(AgentPtr pAgent, SubTree* pSubTree);
		void OnPreExecute() override;
		void OnPostExecute() override;
	private:
		SubTree* m_pSubTree;
		AgentPtr m_pAgent;
		TempMemory m_TempMemory;
	};
}

#endif
