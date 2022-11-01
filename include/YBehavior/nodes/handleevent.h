#ifndef _YBEHAVIOR_PROCESSEVENT_H_
#define _YBEHAVIOR_PROCESSEVENT_H_

#include "YBehavior/treenode.h"

namespace YBehavior
{
	enum struct HandleEventType
	{
		LATEST,
		EVERY,
	};

	class HandleEventContext : public CompositeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
	private:
		StdVector<UINT> m_Hashes;
		StdVector<UINT>* m_pTargetHashes{};
		size_t m_Idx{};
	};
	class HandleEvent : public CompositeNode<HandleEventContext>
	{
		friend HandleEventContext;
	public:
		TREENODE_DEFINE(HandleEvent)
	protected:
		bool OnLoaded(const pugi::xml_node& data) override;
		bool OnLoadFinish() override;
	protected:
		void _GetHashes(StdVector<UINT>& hashes, IMemory* pMemory);

	protected:
		SharedVariableEx<VecString>* m_Events;
		SharedVariableEx<String>* m_Current;
		SharedVariableEx<VecInt>* m_Int;
		SharedVariableEx<VecFloat>* m_Float;
		SharedVariableEx<VecBool>* m_Bool;
		SharedVariableEx<VecUlong>* m_Ulong;
		SharedVariableEx<VecString>* m_String;
		SharedVariableEx<VecEntityWrapper>* m_Entity;
		SharedVariableEx<VecVector3>* m_Vector3;
		
		bool m_bHasParam{};

		StdVector<UINT> m_Hashes;
		HandleEventType m_Type{};
	};
}

#endif
