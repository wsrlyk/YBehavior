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

	class HandleEvent;
	class HandleEventContext : public CompositeNodeContext
	{
	protected:
		void _OnInit() override;
		NodeState _Update(AgentPtr pAgent, NodeState lastState) override;
		NodeState _Return(HandleEvent* pNode, NodeState lastState);
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
		Pin<VecString>* m_Events;
		Pin<String>* m_Current;
		Pin<VecInt>* m_Int;
		Pin<VecFloat>* m_Float;
		Pin<VecBool>* m_Bool;
		Pin<VecUlong>* m_Ulong;
		Pin<VecString>* m_String;
		Pin<VecEntityWrapper>* m_Entity;
		Pin<VecVector3>* m_Vector3;
		
		bool m_bHasParam{};

		StdVector<UINT> m_Hashes;
		HandleEventType m_Type{};
	};
}

#endif
