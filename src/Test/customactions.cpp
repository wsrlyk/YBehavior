#include "customactions.h"
#include "YBehavior/registerdata.h"
#include "YBehavior/behaviortree.h"

void MyLaunchCore::RegisterActions() const
{
	_Register<GetNameAction>();
	_Register<SelectTargetAction>();
	_Register<GetTargetNameAction>();
}

YBehavior::NodeState GetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	YBehavior::NodeState ns = YBehavior::NS_SUCCESS;
	
	LOG_BEGIN << ((XAgent*)pAgent)->GetEntity()->ToString() << LOG_END;


	return ns;
}

YBehavior::KEY XAgent::tickCount0;
YBehavior::KEY XAgent::tickCount1;
YBehavior::KEY XAgent::isfighting;
YBehavior::KEY XAgent::heartrate;
YBehavior::KEY XAgent::isdead;

void XAgent::Update()
{
	YBehavior::RegisterData* pRegister = GetRegister();

	static int i = 1;
	static float f = 0.0f;
	static YBehavior::BOOL b = YBehavior::Utility::FALSE_VALUE;
	
	pRegister->Clear();
	pRegister->SetEvent("hehe");

	if (i > 5)
		i = 1;
	for (int k = 0; k < i; ++k)
	{
		pRegister->Push(k);
	}
	++i;

	if (f > 0.7f)
		f -= 0.7f;
	for (float j = 0.1f; j < f; j += 0.11f)
	{
		pRegister->Push(j);
	}
	f += 0.17f;

	if (b)
	{
		pRegister->Push(YBehavior::Utility::FALSE_VALUE);
		pRegister->Push(YBehavior::Utility::TRUE_VALUE);
	}
	else
	{
		pRegister->Push(YBehavior::Utility::TRUE_VALUE);
	}
	b = !b;

	//YBehavior::VecFloat fftestdata;
	//fftestdata.push_back(1.0f);
	//fftestdata.push_back(2.0f);
	//GetMemory()->GetMainData()->Set<YBehavior::VecFloat>(fftest, fftestdata);

	this->Tick();
}

YBehavior::STRING XEntity::ToString() const
{
	return GetName();
}

YBehavior::NodeState SelectTargetAction::Update(YBehavior::AgentPtr pAgent)
{
	YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Target, true);

	YBehavior::EntityWrapper currentTarget;
	m_Target->GetCastedValue(pAgent->GetMemory(), currentTarget);
	if (currentTarget.IsValid())
	{
		YBehavior::EntityWrapper wrapper;
		m_Target->SetCastedValue(pAgent->GetMemory(), &wrapper);
	}
	else
	{
		YBehavior::EntityWrapper wrapper(pAgent->GetEntity()->GetWrapper());
		m_Target->SetCastedValue(pAgent->GetMemory(), &wrapper);
	}

	YB_LOG_VARIABLE_IF_HAS_DEBUG_POINT(m_Target, false);

	return YBehavior::NS_SUCCESS;
}

bool SelectTargetAction::OnLoaded(const pugi::xml_node& data)
{
	CreateVariable(m_Target, "Target", data, YBehavior::Utility::POINTER_CHAR);
	if (!m_Target)
	{
		return false;
	}

	return true;
}

YBehavior::NodeState GetTargetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	const YBehavior::EntityWrapper* currentTarget = m_Target->GetCastedValue(pAgent->GetMemory());
	if (currentTarget && currentTarget->IsValid())
	{
		LOG_BEGIN << ((XEntity*)currentTarget->Get())->ToString() << LOG_END;
		return YBehavior::NS_SUCCESS;
	}
	else
	{
		LOG_BEGIN << "No Target" << LOG_END;
		return YBehavior::NS_FAILURE;
	}
}

bool GetTargetNameAction::OnLoaded(const pugi::xml_node& data)
{
	CreateVariable(m_Target, "Target", data, YBehavior::Utility::POINTER_CHAR);
	if (!m_Target)
	{
		return false;
	}
	
	YBehavior::ISharedVariableEx* pTestVariable = nullptr;
	CreateVariable(pTestVariable, "TestVariable", data, 0, YBehavior::Utility::GetCreateStr<YBehavior::INT>());

	return true;
}
