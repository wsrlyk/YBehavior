#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/nodefactory.h"
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/sharedvariableex.h"
#ifdef YDEBUGGER
#include "YBehavior/debugger.h"
#endif
#include "YBehavior/shareddataex.h"
#include "YBehavior/agent.h"
#include <string.h>
#include "YBehavior/profile/profileheader.h"


namespace YBehavior
{
	YBehavior::NodeState BehaviorTreeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		BehaviorTree* pTree = (BehaviorTree*)m_pNode;
		if (m_Stage == 0)
		{
			pAgent->GetMemory()->Push(pTree);

			if (m_pLocalMemoryInOut)
				m_pLocalMemoryInOut->OnInput(&pTree->m_Inputs);
		}
//#ifdef YPROFILER
//		profiler.Pause();
//#endif
		NodeState res = SingleChildNodeContext::_Update(pAgent, lastState);

//#ifdef YPROFILER
//		profiler.Resume();
//#endif

		if (m_Stage == 2)
		{
			if (m_pLocalMemoryInOut)
				m_pLocalMemoryInOut->OnOutput(&pTree->m_Outputs);
			///> Pop the local data
			pAgent->GetMemory()->Pop();
		}
		return res;
	}

	BehaviorTree::BehaviorTree(const STRING& name)
	{
		m_TreeNameWithPath = name;
		m_TreeName = Utility::GetNameFromPath(m_TreeNameWithPath);

		m_SharedData = new SharedDataEx();
		m_LocalData = nullptr;
		//m_NameKeyMgr = new NameKeyMgr();
	}

	BehaviorTree::~BehaviorTree()
	{
		delete m_SharedData;
		if (m_LocalData)
		{
			delete m_LocalData;
			m_LocalData = nullptr;
		}
		//delete m_NameKeyMgr;
	}

	bool BehaviorTree::OnLoadChild(const pugi::xml_node& data)
	{
		///> Shared & Local Variables
		if (strcmp(data.name(), "Shared") == 0 || strcmp(data.name(), "Local") == 0)
		{
			StdVector<STRING> buffer;

			for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
			{
				if (KEY_WORDS.count(it->name()))
					continue;
				if (!ParseVariable(*it, data, buffer, ST_NONE))
					return false;
				const ISharedVariableCreateHelper* helper = SharedVariableCreateHelperMgr::Get(buffer[0].substr(0, 2));
				if (helper == nullptr)
					continue;

				if (buffer[0][2] == Utility::CONST_CHAR)
					helper->SetSharedData(m_SharedData, it->name(), buffer[1]);
				else
					helper->SetSharedData(GetLocalData(), it->name(), buffer[1]);
			}
		}
		///> Inputs & Outputs
		else if (strcmp(data.name(), "Input") == 0 || strcmp(data.name(), "Output") == 0)
		{
			std::unordered_map<STRING, ISharedVariableEx*>& container = data.name()[0] == 'I' ? m_Inputs : m_Outputs;
			for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
			{
				ISharedVariableEx* pVariable = nullptr;

				CreateVariable(pVariable, it->name(), data, ST_NONE);
				if (!pVariable)
				{
					ERROR_BEGIN_NODE_HEAD << "Failed to Create " << data.name() << ERROR_END;
					return false;
				}
				if (container.count(it->name()) > 0)
				{
					ERROR_BEGIN_NODE_HEAD << "Duplicate " << data.name() << " Variable: " << it->name() << ERROR_END;
					return false;
				}
				container[it->name()] = pVariable;
			}
		}
		return true;
	}

	YBehavior::SharedDataEx* BehaviorTree::GetLocalData()
	{
		if (!m_LocalData)
			m_LocalData = new SharedDataEx();
		return m_LocalData;
	}

	void BehaviorTree::MergeDataTo(SharedDataEx& destination)
	{
		destination.MergeFrom(*m_SharedData, false);
	}


	YBehavior::NodeState BehaviorTree::RootExecute(AgentPtr pAgent, NodeState parentState, LocalMemoryInOut* pInOut)
	{
#ifdef YPROFILER
		Profiler::TreeProfileHelper profiler(this);
#endif
		///> Push the local data to the stack of the agent memory
		if (parentState != NS_RUNNING)
		{
			pAgent->GetMemory()->Push(this);

			if (pInOut)
				pInOut->OnInput(&m_Inputs);
		}
#ifdef YPROFILER
		profiler.Pause();
#endif
		NodeState res = Execute(pAgent, parentState);

#ifdef YPROFILER
		profiler.Resume();
#endif

		if (res != NS_RUNNING)
		{
			if (pInOut)
				pInOut->OnOutput(&m_Outputs);
			///> Pop the local data
			pAgent->GetMemory()->Pop();
		}

		return res;
	}

	YBehavior::TreeNodeContext* BehaviorTree::CreateRootContext(LocalMemoryInOut* pTunnel /*= nullptr*/)
	{
		NodeContextType* pContext = (NodeContextType*)CreateContext();
		pContext->Init(pTunnel);
		return pContext;
	}

	//////////////////////////////////////////////////////////////////////////
	//////////////////////////////////////////////////////////////////////////


	LocalMemoryInOut::LocalMemoryInOut(AgentPtr pAgent, std::vector<ISharedVariableEx* >* pInputsFrom, std::vector<ISharedVariableEx* >* pOutputsTo)
	{
		Set(pAgent, pInputsFrom, pOutputsTo);
	}


	void LocalMemoryInOut::Set(AgentPtr pAgent, std::vector<ISharedVariableEx* >* pInputsFrom, std::vector<ISharedVariableEx* >* pOutputsTo)
	{
		m_pAgent = pAgent;
		m_pInputsFrom = pInputsFrom;
		m_pOutputsTo = pOutputsTo;
		m_TempMemory.Set(pAgent->GetMemory()->GetMainData(), pAgent->GetMemory()->GetStackTop());
	}

	void LocalMemoryInOut::OnInput(std::unordered_map<STRING, ISharedVariableEx*>* pInputsTo)
	{
		if (m_pInputsFrom && pInputsTo)
		{
			for (auto it = m_pInputsFrom->begin(); it != m_pInputsFrom->end(); ++it)
			{
				ISharedVariableEx* pFrom = *it;
				auto it2 = pInputsTo->find(pFrom->GetName());
				if (it2 == pInputsTo->end())
					continue;
				ISharedVariableEx* pTo = it2->second;
				if (pFrom->TypeID() != pTo->TypeID())
				{
					ERROR_BEGIN << "From & To Types not match: " << pFrom->GetLogName() << ", at main tree: " << m_pAgent->GetRunningTree()->GetTreeName() << ERROR_END;
					continue;
				}
				pTo->SetValue(m_pAgent->GetMemory(), pFrom->GetValue(&m_TempMemory));
			}
		}
	}

	void LocalMemoryInOut::OnOutput(std::unordered_map<STRING, ISharedVariableEx*>* pOutputsFrom)
	{
		if (m_pOutputsTo && pOutputsFrom)
		{
			for (auto it = m_pOutputsTo->begin(); it != m_pOutputsTo->end(); ++it)
			{
				ISharedVariableEx* pTo = *it;
				auto it2 = pOutputsFrom->find(pTo->GetName());
				if (it2 == pOutputsFrom->end())
					continue;
				ISharedVariableEx* pFrom = it2->second;
				if (pFrom->TypeID() != pTo->TypeID())
				{
					ERROR_BEGIN << "From & To Types not match: " << pFrom->GetLogName() << ", at main tree: " << m_pAgent->GetRunningTree()->GetTreeName() << ERROR_END;
					continue;
				}
				pTo->SetValue(&m_TempMemory, pFrom->GetValue(m_pAgent->GetMemory()));
			}
		}
	}
}