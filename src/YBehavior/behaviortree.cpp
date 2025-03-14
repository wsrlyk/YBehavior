#include "YBehavior/behaviortree.h"
#include "YBehavior/3rd/pugixml/pugixml.hpp"
#include "YBehavior/logger.h"
#include "YBehavior/utility.h"
#include <sstream>
#include "YBehavior/datacreatehelper.h"
#include "YBehavior/pin.h"
#ifdef YDEBUGGER
#include "YBehavior/debugger.h"
#endif
#include "YBehavior/variable.h"
#include "YBehavior/agent.h"
#include <cstring>
#ifdef YPROFILER
#include "YBehavior/profile/profileheader.h"
#endif
#include "YBehavior/pincreation.h"
#include <set>

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

		m_SharedData = new VariableCollection();
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
				if (!PinCreation::ParsePin(this, *it, data, buffer, ST_NONE))
					return false;
				const IDataCreateHelper* helper = DataCreateHelperMgr::Get(buffer[0].substr(0, 2));
				if (helper == nullptr)
					continue;

				if (buffer[0][2] == Utility::CONST_CHAR)
					helper->SetVariable(m_SharedData, it->name(), buffer[1]);
				else
					helper->SetVariable(GetLocalData(), it->name(), buffer[1]);
			}
		}
		///> Inputs & Outputs
		else if (strcmp(data.name(), "Input") == 0 || strcmp(data.name(), "Output") == 0)
		{
			bool isInput = data.name()[0] == 'I';
			auto& container = isInput ? m_Inputs : m_Outputs;
			for (auto it = data.attributes_begin(); it != data.attributes_end(); ++it)
			{
				IPin* pPin = nullptr;

				PinCreation::CreatePin(this, pPin, it->name(), data, isInput ? PinCreation::Flag::None : PinCreation::Flag::IsOutput);
				if (!pPin)
				{
					ERROR_BEGIN_NODE_HEAD << "Failed to Create " << data.name() << ERROR_END;
					return false;
				}
				//if (container.find(it->name()) != container.end())
				//{
				//	ERROR_BEGIN_NODE_HEAD << "Duplicate " << data.name() << " Variable: " << it->name() << ERROR_END;
				//	return false;
				//}
				container.emplace_back(pPin);
			}
		}

		return true;
	}

	YBehavior::VariableCollection* BehaviorTree::GetLocalData()
	{
		if (!m_LocalData)
			m_LocalData = new VariableCollection();
		return m_LocalData;
	}

	void BehaviorTree::RegiseterEvent(UINT e, UINT count)
	{
		m_ValidEvents.insert({ e, count });
	}

	void BehaviorTree::AddTreeNodeCount(const STRING& name)
	{
		auto& count = m_TreeNodeCounts[name];
		++count;
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

		///> FromUID, FromPinIndex, ToUID, ToPinIndex
		using Range = std::tuple<UINT, UINT, UINT, UINT>;
		struct Ranges
		{
			std::vector<Range> ranges;
			KEY key;
		};
		small_map<TYPEID, std::vector<Ranges>> usedRanges;

		int lastRangesListIndex = -1;
		UINT lastFromUID = 0;
		UINT lastFromPinIndex = 0;

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
			auto fromPin = fromNode->GetPin(fromName);
			if (!fromPin)
			{
				ERROR_BEGIN << "DataConnection invalid FromName " << fromName << ERROR_END;
				return false;
			}
			auto toNode = treeNodeCache[toUID];
			auto toPin = toNode->GetPin(toName);
			if (!toPin)
			{
				ERROR_BEGIN << "DataConnection invalid ToName " << toName << ERROR_END;
				return false;
			}

			TYPEID typeID = fromPin->TypeID();
			if (toPin->TypeID() != typeID)
			{
				ERROR_BEGIN << "DataConnection Different types: " << fromName << " & " << toName << ERROR_END;
				return false;
			}
			
			auto setCurrentKey = [&fromPin, &toPin, this](KEY key)
			{
				fromPin->SetIsLocal(true);
				fromPin->SetKey(key);
				toPin->SetIsLocal(true);
				toPin->SetKey(key);
				GetLocalData()->SetDefault(key, fromPin->TypeID());
			};
			auto setLastInfos = [&lastFromUID, &lastFromPinIndex, &lastRangesListIndex, fromUID, fromPin](int rangesListIndex)
			{
				lastFromUID = fromUID;
				lastFromPinIndex = fromPin->GetIndex();
				lastRangesListIndex = rangesListIndex;
			};

			auto it2 = usedRanges.find(typeID);
			///> It's the first data in this type
			if (it2 == usedRanges.end())
			{
				std::vector<Ranges> rangesList;
				Ranges ranges;
				ranges.key = ++currentKey;
				ranges.ranges.emplace_back(fromUID, fromPin->GetIndex(), toUID, toPin->GetIndex());
				rangesList.emplace_back(ranges);
				usedRanges[typeID] = rangesList;

				setCurrentKey(currentKey);
				setLastInfos(0);

				continue;
			}
			{
				/// We assume that these ranges are feed IN ORDER
				/// So current FromUID can only be equal or larger than the previous FromUIDs
				/// And previous ranges cant make holes
				
				std::vector<Ranges>& rangesList = it2->second;

				///> In this case, an output is connected to multiple inputs. Just merge them.
				if (lastFromUID == fromUID && lastFromPinIndex == fromPin->GetIndex())
				{
					///> It must be the last range cause of ORDER
					Ranges& ranges = rangesList[lastRangesListIndex];
					Range& range = *ranges.ranges.rbegin();
					///> Use the larger range
					if (toUID > std::get<2>(range))
					{
						std::get<2>(range) = toUID;
						std::get<3>(range) = toPin->GetIndex();
					}
					setCurrentKey(ranges.key);
					continue;
				}

				int validRangesListIndex = -1;

				bool finish = false;
				for (int i = 0, imax = (int)rangesList.size(); i < imax; ++i)
				{
					Ranges& ranges = rangesList[i];
					bool isValid = true;
					for (int j = 0, jmax = (int)ranges.ranges.size(); j < jmax; ++j)
					{
						Range& range = ranges.ranges[j];
						///> Totally not in this range.
						///  It can be appended to this range, if no better ranges exist.
						if (fromUID > std::get<2>(range))
						{
							continue;
						}

						///> Merge
						if (fromUID == std::get<2>(range))
						{
							std::get<2>(range) = toUID;
							std::get<3>(range) = toPin->GetIndex();
							setCurrentKey(ranges.key);
							setLastInfos(i);
							finish = true;
							break;
						}

						if (fromUID == std::get<0>(range))
						{
							///> A pin is connected with two more nodes, merge them
							if (std::get<1>(range) == fromPin->GetIndex())
							{
								///> Use the larger range
								if (toUID > std::get<2>(range))
								{
									std::get<2>(range) = toUID;
									std::get<3>(range) = toPin->GetIndex();
								}
								setCurrentKey(ranges.key);
								setLastInfos(i);
								finish = true;
							}
							isValid = false;
							break;
						}

						///> All other cases are invalid

						///> This rangesList is not fit for this connect;
						isValid = false;
						break;
					}

					if (finish)
						break;
					if (isValid)
						validRangesListIndex = i;
				}

				if (!finish)
				{
					///> append to this range
					if (validRangesListIndex >= 0)
					{
						Ranges& ranges = rangesList[validRangesListIndex];
						ranges.ranges.emplace_back(fromUID, fromPin->GetIndex(), toUID, toPin->GetIndex());
						setCurrentKey(ranges.key);
						setLastInfos(validRangesListIndex);
					}
					///> Need a new KEY for this range
					else
					{
						Ranges ranges;
						ranges.key = ++currentKey;
						ranges.ranges.emplace_back(fromUID, fromPin->GetIndex(), toUID, toPin->GetIndex());
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


	LocalMemoryInOut::LocalMemoryInOut(AgentPtr pAgent, std::vector<IPin* >* pInputsFrom, std::vector<IPin* >* pOutputsTo)
	{
		Set(pAgent, pInputsFrom, pOutputsTo);
	}


	void LocalMemoryInOut::Set(AgentPtr pAgent, std::vector<IPin* >* pInputsFrom, std::vector<IPin* >* pOutputsTo)
	{
		m_pAgent = pAgent;
		m_pInputsFrom = pInputsFrom;
		m_pOutputsTo = pOutputsTo;
		m_TempMemory.Set(pAgent->GetMemory()->GetMainData(), pAgent->GetMemory()->GetStackTop());
	}

	void LocalMemoryInOut::OnInput(StdVector<IPin*>* pInputsTo)
	{
		if (m_pInputsFrom && pInputsTo && m_pInputsFrom->size() == pInputsTo->size())
		{
			auto len = m_pInputsFrom->size();
			for (size_t i = 0; i < len; ++i)
			{
				IPin* pFrom = (*m_pInputsFrom)[i];
				IPin* pTo = (*pInputsTo)[i];
				if (!pFrom || !pTo)
					continue;
				if (pFrom->TypeID() != pTo->TypeID())
				{
					ERROR_BEGIN << "From & To Types not match: " << pFrom->GetLogName() << ", at main tree: " << m_pAgent->GetRunningTree()->GetTreeName() << ERROR_END;
					continue;
				}
				pTo->SetValue(m_pAgent->GetMemory(), pFrom->GetValuePtr(&m_TempMemory));
			}
		}
	}

	void LocalMemoryInOut::OnOutput(StdVector<IPin*>* pOutputsFrom)
	{
		if (m_pOutputsTo && pOutputsFrom && m_pOutputsTo->size() == pOutputsFrom->size())
		{
			auto len = m_pOutputsTo->size();
			for (size_t i = 0; i < len; ++i)
			{
				IPin* pTo = (*m_pOutputsTo)[i];
				IPin* pFrom = (*pOutputsFrom)[i];
				if (!pFrom || !pTo)
					continue;
				if (pFrom->TypeID() != pTo->TypeID())
				{
					ERROR_BEGIN << "From & To Types not match: " << pFrom->GetLogName() << ", at main tree: " << m_pAgent->GetRunningTree()->GetTreeName() << ERROR_END;
					continue;
				}
				pTo->SetValue(&m_TempMemory, pFrom->GetValuePtr(m_pAgent->GetMemory()));
			}
		}
	}
}