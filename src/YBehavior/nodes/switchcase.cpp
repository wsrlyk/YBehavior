#include "YBehavior/nodes/switchcase.h"
#include "YBehavior/agent.h"
#include "YBehavior/profile/profileheader.h"

namespace YBehavior
{
	static std::unordered_set<TYPEID> s_ValidTypes = {
		GetTypeID<Int>(),
		GetTypeID<Float>(),
		GetTypeID<Bool>(),
		GetTypeID<String>(),
		GetTypeID<Uint64>(),
	};

	static std::unordered_set<TYPEID> s_ValidVecTypes = {
		GetTypeID<VecInt>(),
		GetTypeID<VecFloat>(),
		GetTypeID<VecBool>(),
		GetTypeID<VecString>(),
		GetTypeID<VecUint64>(),
	};

	bool SwitchCase::OnLoaded(const pugi::xml_node& data)
	{
		TYPEID switchType = CreateVariable(m_Switch, "Switch", data);
		if (s_ValidTypes.find(switchType) == s_ValidTypes.end())
		{
			ERROR_BEGIN_NODE_HEAD << "Invalid type for Switch: " << switchType << ERROR_END;
			return false;
		}
		TYPEID casesType = CreateVariable(m_Cases, "Cases", data);
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
			if ((INT)pNode->m_CasesChilds.size() != pNode->m_Cases->VectorSize(pAgent->GetMemory()))
			{
				return NS_FAILURE;
			}

			INT size = (INT)pNode->m_CasesChilds.size();
			TreeNodePtr targetNode = nullptr;
			IVariableOperationHelper* pHelper = pNode->m_Switch->GetOperation();

			for (INT i = 0; i < size; ++i)
			{
				const void* onecase = pNode->m_Cases->GetElement(pAgent->GetMemory(), i);
				if (onecase == nullptr)
					continue;

				if (pHelper->Compare(pNode->m_Switch->GetValue(pAgent->GetMemory()), onecase, OT_EQUAL))
				{
					targetNode = pNode->m_CasesChilds[i];
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