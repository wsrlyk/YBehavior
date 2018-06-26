#include "customactions.h"
#include "YBehavior/registerdata.h"
#include "YBehavior/behaviortree.h"
#ifdef DEBUGGER
#include "YBehavior/debugger.h"
#endif // DEBUGGER

void MyLaunchCore::RegisterActions() const
{
	_Register<GetNameAction>();
	_Register<SelectTargetAction>();
	_Register<GetTargetNameAction>();
}

YBehavior::NodeState GetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	YBehavior::NodeState ns = YBehavior::NS_SUCCESS;
	
	LOG_BEGIN << ((XAgent*)pAgent)->GetEntity()->GetName() << LOG_END;


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
	static YBehavior::BOOL b = YBehavior::FALSE;
	
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
		pRegister->Push(YBehavior::FALSE);
		pRegister->Push(YBehavior::TRUE);
	}
	else
	{
		pRegister->Push(YBehavior::TRUE);
	}
	b = !b;

	this->Tick();
}

YBehavior::STRING XAgent::ToString() const
{
	return m_pEntity->GetName();
}

YBehavior::NodeState SelectTargetAction::Update(YBehavior::AgentPtr pAgent)
{
	LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Target, true);

	const YBehavior::AgentWrapper* currentTarget = m_Target->GetCastedValue(pAgent->GetSharedData());
	if (currentTarget->IsValid())
	{
		YBehavior::AgentWrapper wrapper;
		m_Target->SetCastedValue(pAgent->GetSharedData(), &wrapper);
	}
	else
	{
		YBehavior::AgentWrapper wrapper(pAgent->CreateWrapper());
		m_Target->SetCastedValue(pAgent->GetSharedData(), &wrapper);
	}

	LOG_SHARED_DATA_IF_HAS_LOG_POINT(m_Target, false);

	return YBehavior::NS_SUCCESS;
}

void SelectTargetAction::OnLoaded(const pugi::xml_node& data)
{
	YBehavior::TYPEID typeID = CreateVariable(m_Target, "Target", data, true, YBehavior::POINTER);
	if (typeID != YBehavior::GetClassTypeNumberId<YBehavior::AgentWrapper>())
	{
		ERROR_BEGIN << "Type of [Target] Error in SelectTargetAction" << ERROR_END;
	}

}

YBehavior::NodeState GetTargetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	const YBehavior::AgentWrapper* currentTarget = m_Target->GetCastedValue(pAgent->GetSharedData());
	if (currentTarget->IsValid())
	{
		LOG_BEGIN << ((XAgent*)currentTarget->Get())->GetEntity()->GetName() << LOG_END;
	}
	else
	{
		LOG_BEGIN << "No Target" << LOG_END;
	}

	return YBehavior::NS_SUCCESS;
}

void GetTargetNameAction::OnLoaded(const pugi::xml_node& data)
{
	YBehavior::TYPEID typeID = CreateVariable(m_Target, "Target", data, true, YBehavior::POINTER);
	if (typeID != YBehavior::GetClassTypeNumberId<YBehavior::AgentWrapper>())
	{
		ERROR_BEGIN << "Type of [Target] Error in SelectTargetAction" << ERROR_END;
	}
}
