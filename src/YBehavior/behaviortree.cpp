#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/sharedvariableex.h"
#ifdef YDEBUGGER
#include "YBehavior/debugger.h"
#endif
#include "YBehavior/shareddataex.h"
#include "YBehavior/agent.h"
#include <cstring>
#ifdef YPROFILER
#include "YBehavior/profile/profileheader.h"
#endif
#include "YBehavior/variablecreation.h"


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
		m_ClassName = "Root";
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

	static std::set<STRING> KEY_WORDS{ "Class", "Pos", "NickName" };
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
				if (!VariableCreation::ParseVariable(this, *it, data, buffer, ST_NONE))
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
			auto& container = data.name()[0] == 'I' ? m_Inputs : m_Outputs;
			for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
			{
				ISharedVariableEx* pVariable = nullptr;

				VariableCreation::CreateVariable(this, pVariable, it->name(), data, ST_NONE);
				if (!pVariable)
				{
					ERROR_BEGIN_NODE_HEAD << "Failed to Create " << data.name() << ERROR_END;
					return false;
				}
				//if (container.find(it->name()) != container.end())
				//{
				//	ERROR_BEGIN_NODE_HEAD << "Duplicate " << data.name() << " Variable: " << it->name() << ERROR_END;
				//	return false;
				//}
				container.emplace_back(pVariable);
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
		destination.MergeFrom(*m_SharedData, true);
	}

	YBehavior::TreeNodeContext* BehaviorTree::CreateRootContext(LocalMemoryInOut* pTunnel /*= nullptr*/)
	{
		NodeContextType* pContext = (NodeContextType*)CreateContext();
		pContext->Init(pTunnel);
		return pContext;
	}

	bool BehaviorTree::ProcessDataConnections(const std::vector<TreeNode*>& treeNodeCache, const pugi::xml_node& data)
	{
		if (data.empty())
			return true;

		static const KEY TEMP_KEY_OFFSET = 1000000;
		KEY currentKey = TEMP_KEY_OFFSET;

		///> FromUID, FromVariableIndex, ToUID, ToVariableIndex
		using Range = std::tuple<UINT, UINT, UINT, UINT>;
		struct Ranges
		{
			std::vector<Range> ranges;
			KEY key;
		};
		small_map<TYPEID, std::vector<Ranges>> usedRanges;

		int lastRangesListIndex = -1;
		UINT lastFromUID = 0;
		UINT lastFromVariableIndex = 0;

		for (auto it = data.begin(); it != data.end(); ++it)
		{
			UINT fromUID = it->attribute("FromUID").as_uint(0);
			UINT toUID = it->attribute("ToUID").as_uint(0);
			STRING fromName = it->attribute("FromName").value();
			STRING toName = it->attribute("ToName").value();

			if (fromUID == 0 || fromUID >= treeNodeCache.size())
			{
				ERROR_BEGIN << "DataConnection invalid FromUID " << fromUID << ERROR_END;
				return false;
			}
			if (toUID == 0 || toUID >= treeNodeCache.size())
			{
				ERROR_BEGIN << "DataConnection invalid ToUID " << toUID << ERROR_END;
				return false;
			}

			if (fromUID >= toUID)
			{
				ERROR_BEGIN << "DataConnection FromUID MUST larger than ToUID " << fromName << " & " << toUID << ERROR_END;
				return false;
			}

			auto fromNode = treeNodeCache[fromUID];
			auto fromVariable = fromNode->GetVariable(fromName);
			if (!fromVariable)
			{
				ERROR_BEGIN << "DataConnection invalid FromName " << fromName << ERROR_END;
				return false;
			}
			auto toNode = treeNodeCache[toUID];
			auto toVariable = toNode->GetVariable(toName);
			if (!toVariable)
			{
				ERROR_BEGIN << "DataConnection invalid ToName " << toName << ERROR_END;
				return false;
			}

			TYPEID typeID = fromVariable->TypeID();
			if (toVariable->TypeID() != typeID)
			{
				ERROR_BEGIN << "DataConnection Different types: " << fromName << " & " << toName << ERROR_END;
				return false;
			}
			
			auto setCurrentKey = [&fromVariable, &toVariable, this](KEY key)
			{
				fromVariable->SetIsLocal(true);
				fromVariable->SetKey(key);
				toVariable->SetIsLocal(true);
				toVariable->SetKey(key);
				GetLocalData()->SetDefault(key, fromVariable->TypeID());
			};
			auto setLastInfos = [&lastFromUID, &lastFromVariableIndex, &lastRangesListIndex, fromUID, fromVariable](int rangesListIndex)
			{
				lastFromUID = fromUID;
				lastFromVariableIndex = fromVariable->GetIndex();
				lastRangesListIndex = rangesListIndex;
			};

			auto it2 = usedRanges.find(typeID);
			///> It's the first data in this type
			if (it2 == usedRanges.end())
			{
				std::vector<Ranges> rangesList;
				Ranges ranges;
				ranges.key = ++currentKey;
				ranges.ranges.emplace_back(fromUID, fromVariable->GetIndex(), toUID, toVariable->GetIndex());
				rangesList.emplace_back(ranges);
				usedRanges[typeID] = rangesList;

				setCurrentKey(currentKey);
				setLastInfos(0);

				continue;
			}
			{
				/// We assume that these ranges are feeded IN ORDER
				/// So current FromUID can only be equal or larger than the previous FromUIDs
				/// And previous ranges cant make holes
				
				std::vector<Ranges>& rangesList = it2->second();

				///> In this case, an output is connected to multiple inputs. Just merge them.
				if (lastFromUID == fromUID && lastFromVariableIndex == fromVariable->GetIndex())
				{
					///> It must be the last range cause of ORDER
					Ranges& ranges = rangesList[lastRangesListIndex];
					Range& range = *ranges.ranges.rbegin();
					///> Use the larger range
					if (toUID > std::get<2>(range))
					{
						std::get<2>(range) = toUID;
						std::get<3>(range) = toVariable->GetIndex();
					}
					setCurrentKey(ranges.key);
					continue;
				}

				int validRangesListIndex = -1;

				bool finish = false;
				for (int i = 0, imax = (int)rangesList.size(); i < imax; ++i)
				{
					Ranges& ranges = rangesList[i];
					for (int j = 0, jmax = (int)ranges.ranges.size(); j < jmax; ++j)
					{
						Range& range = ranges.ranges[j];
						///> Totally not in this range.
						///  It can be appended to this range, if no better ranges exist.
						if (fromUID > std::get<2>(range))
						{
							validRangesListIndex = i;
							continue;
						}

						///> Merge
						if (fromUID == std::get<2>(range))
						{
							std::get<2>(range) = toUID;
							std::get<3>(range) = toVariable->GetIndex();
							setCurrentKey(ranges.key);
							setLastInfos(i);
							finish = true;
							break;
						}

						if (fromUID == std::get<0>(range))
						{
							///> A variable is connected with two more nodes, merge them
							if (std::get<1>(range) == fromVariable->GetIndex())
							{
								///> Use the larger range
								if (toUID > std::get<2>(range))
								{
									std::get<2>(range) = toUID;
									std::get<3>(range) = toVariable->GetIndex();
								}
								setCurrentKey(ranges.key);
								setLastInfos(i);
								finish = true;
							}
							break;
						}

						///> All other cases are invalid

						///> This rangesList is not fit for this connect;
						if (validRangesListIndex == i)
							validRangesListIndex = -1;
						break;
					}

					if (finish)
						break;
				}

				if (!finish)
				{
					///> append to this range
					if (validRangesListIndex >= 0)
					{
						Ranges& ranges = rangesList[validRangesListIndex];
						ranges.ranges.emplace_back(fromUID, fromVariable->GetIndex(), toUID, toVariable->GetIndex());
						setCurrentKey(ranges.key);
						setLastInfos(validRangesListIndex);
					}
					///> Need a new KEY for this range
					else
					{
						Ranges ranges;
						ranges.key = ++currentKey;
						ranges.ranges.emplace_back(fromUID, fromVariable->GetIndex(), toUID, toVariable->GetIndex());
						rangesList.emplace_back(ranges);

						setCurrentKey(currentKey);
						setLastInfos((int)rangesList.size() - 1);
					}
				}
			}
		}

		return true;
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

	void LocalMemoryInOut::OnInput(StdVector<ISharedVariableEx*>* pInputsTo)
	{
		if (m_pInputsFrom && pInputsTo && m_pInputsFrom->size() == pInputsTo->size())
		{
			auto len = m_pInputsFrom->size();
			for (size_t i = 0; i < len; ++i)
			{
				ISharedVariableEx* pFrom = (*m_pInputsFrom)[i];
				ISharedVariableEx* pTo = (*pInputsTo)[i];
				if (!pFrom || !pTo)
					continue;
				if (pFrom->TypeID() != pTo->TypeID())
				{
					ERROR_BEGIN << "From & To Types not match: " << pFrom->GetLogName() << ", at main tree: " << m_pAgent->GetRunningTree()->GetTreeName() << ERROR_END;
					continue;
				}
				pTo->SetValue(m_pAgent->GetMemory(), pFrom->GetValue(&m_TempMemory));
			}
		}
	}

	void LocalMemoryInOut::OnOutput(StdVector<ISharedVariableEx*>* pOutputsFrom)
	{
		if (m_pOutputsTo && pOutputsFrom && m_pOutputsTo->size() == pOutputsFrom->size())
		{
			auto len = m_pOutputsTo->size();
			for (size_t i = 0; i < len; ++i)
			{
				ISharedVariableEx* pTo = (*m_pOutputsTo)[i];
				ISharedVariableEx* pFrom = (*pOutputsFrom)[i];
				if (!pFrom || !pTo)
					continue;
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