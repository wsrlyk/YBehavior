#include "switchcase.h"
#include "YBehavior/agent.h"
#include "YBehavior/pincreation.h"
#include "../operations/datacompare.h"
#include <set>
#include "YBehavior/fsm/context.h"

namespace YBehavior
{
	static std::set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Bool>(),
		GetTypeID<String>(),
		GetTypeID<Uint64>(),
	};

	static std::set<TYPEID> s_ValidVecTypes = {
		GetTypeID<VecInt>(),
		GetTypeID<VecFloat>(),
		GetTypeID<VecBool>(),
		GetTypeID<VecString>(),
		GetTypeID<VecUint64>(),
	};

	bool SwitchCase::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID switchType = PinCreation::CreatePin(this, m_Switch, "Switch", data);
		if (s_ValidTypes.find(switchType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Switch: " << switchType << ERROR_END;
			return false;
		}
		TYPEID casesType = PinCreation::CreatePin(this, m_Cases, "Cases", data);
		if (s_ValidVecTypes.find(casesType) == s_ValidVecTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Cases: " << casesType << ERROR_END;
			return false;
		}

		if (!Utility::IsElement(switchType, casesType))
		{
			ERROR_BEGIN_NODE_HEAD << "Different types: Switch & Cases " << ERROR_END;
			return false;
		}
		
		m_pHelper = DataCompareMgr::Instance()->Get(switchType);
		if (!m_pHelper)
		{
			return false;
		}

		return true;
	}

	void SwitchCase::OnAddChild(TreeNode * child, const STRING & connection)
	{
		if (connection == "default")
		{
			if (m_DefaultChild != nullptr)
			{
				ERROR_BEGIN_NODE_HEAD << "Too many default case" << ERROR_END;
			}
			else
			{
				m_DefaultChild = child;
			}
		}
		else
		{
			m_CasesChilds.push_back(child);
		}
	}

	YBehavior::NodeState SwitchCaseNodeContext::_Update(AgentPtr pAgent, NodeState lastState)
	{
		SwitchCase* pNode = (SwitchCase*)m_pNode;
		if (m_Stage == 0)
		{
			++m_Stage;
			YB_IF_HAS_DEBUG_POINT
			{
				YB_LOG_PIN_BEFORE(pNode->m_Switch);
				YB_LOG_PIN_BEFORE(pNode->m_Cases);
			}
			INT childsize = (INT)pNode->m_CasesChilds.size();
			INT casesize = pNode->m_Cases->ArraySize(pAgent->GetMemory());
			if (childsize != 1 && childsize != casesize)
			{
				YB_LOG_INFO_WITH_END("Cases size != Children size");
				return NS_FAILURE;
			}
			TreeNodePtr targetNode = nullptr;
			auto pSwitchValue = pNode->m_Switch->GetValuePtr(pAgent->GetMemory());
			for (INT i = 0; i < casesize; ++i)
			{
				const void* onecase = pNode->m_Cases->GetElementPtr(pAgent->GetMemory(), i);
				if (onecase == nullptr)
					continue;

				if (pNode->m_pHelper->Compare(pSwitchValue, onecase, CompareType::EQUAL))
				{
					targetNode = pNode->m_CasesChilds[childsize == 1 ? 0 : i];
					break;
				}
			}
			if (targetNode == nullptr)
			{
				targetNode = pNode->m_DefaultChild;
			}

			if (targetNode == nullptr)
				return NS_FAILURE;

			pAgent->GetTreeContext()->PushCallStack(targetNode->CreateContext());
			return NS_RUNNING;
		}
		else
		{
			return lastState;
		}
	}

}