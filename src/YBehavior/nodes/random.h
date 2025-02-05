#ifndef _YBEHAVIOR_RANDOM_H_
#define _YBEHAVIOR_RANDOM_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	class IDataRandomHelper;
	class Random : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Random)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		IPin* m_Bound1;
		IPin* m_Bound2;
		IPin* m_Target;

		const IDataRandomHelper* m_pHelper;
	};

	class RandomSelect : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(RandomSelect)
	protected:
		NodeState Update(AgentPtr pAgent) override;
		bool OnLoaded(const pugi::xml_node& data) override;

	private:
		IPin* m_Input;
		IPin* m_Output;

		TYPEID m_DataType;
	};

	class IDataOperationHelper;
	class Shuffle : public LeafNode<>
	{
	public:
		TREENODE_DEFINE(Shuffle)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		NodeState Update(AgentPtr pAgent) override;

	private:
		IPin* m_Input;
		IPin* m_Output;

		bool m_bSameArray;
		const IDataOperationHelper* m_pHelper;
	};

}

#endif
