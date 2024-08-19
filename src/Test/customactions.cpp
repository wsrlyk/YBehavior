#include "customactions.h"
#include "YBehavior/eventqueue.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/pincreation.h"

void MyLaunchCore::RegisterActions() const
{
	_Register<GetNameAction>();
	_Register<SelectTargetAction>();
	_Register<GetTargetNameAction>();
	_Register<ProjectVector3>();
	_Register<SetVector3>();
}

YBehavior::NodeState GetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	YBehavior::NodeState ns = YBehavior::NS_SUCCESS;
	
	////LOG_BEGIN << ((XAgent*)pAgent)->GetEntity()->ToString() << LOG_END;


	return ns;
}

YBehavior::KEY XAgent::tickCount0;
YBehavior::KEY XAgent::tickCount1;
YBehavior::KEY XAgent::isfighting;
YBehavior::KEY XAgent::heartrate;
YBehavior::KEY XAgent::isdead;

void XAgent::Update()
{
	auto pQueue = this->GetEventQueue();

	static int i = 1;
	static float f = 0.0f;
	static bool b = false;
	
	if (i > 5)
	{
		i = 1;
		pQueue->ClearAll();
	}
	else
		pQueue->Clear();


	if (auto pData = pQueue->Create("hehe"))
	{
		pData->notClear = b;
		for (int k = 0; k < i; ++k)
		{
			pData->Push(k);
		}
		++i;

		if (f > 0.7f)
			f -= 0.7f;
		for (float j = 0.1f; j < f; j += 0.11f)
		{
			pData->Push(j);
		}
		f += 0.17f;

		if (b)
		{
			pData->Push(YBehavior::Utility::FALSE_VALUE);
			pData->Push(YBehavior::Utility::TRUE_VALUE);
		}
		else
		{
			pData->Push(YBehavior::Utility::TRUE_VALUE);
		}
	}
	{
		pQueue->Create("haha");
		pQueue->Create("hoho");
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
	YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Target, true);

	YBehavior::EntityWrapper currentTarget;
	m_Target->GetValue(pAgent->GetMemory(), currentTarget);
	if (currentTarget.IsValid())
	{
		YBehavior::EntityWrapper wrapper;
		m_Target->SetValue(pAgent->GetMemory(), &wrapper);
	}
	else
	{
		YBehavior::EntityWrapper wrapper(pAgent->GetEntity()->GetWrapper());
		m_Target->SetValue(pAgent->GetMemory(), &wrapper);
	}

	YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Target, false);

	return YBehavior::NS_SUCCESS;
}

bool SelectTargetAction::OnLoaded(const pugi::xml_node& data)
{
	YB::PinCreation::CreatePin(this, m_Target, "Target", data, YBehavior::PinCreation::Flag::IsOutput);
	if (!m_Target)
	{
		return false;
	}

	return true;
}

YBehavior::NodeState GetTargetNameAction::Update(YBehavior::AgentPtr pAgent)
{
	const YBehavior::EntityWrapper* currentTarget = m_Target->GetValue(pAgent->GetMemory());
	if (currentTarget && currentTarget->IsValid())
	{
		////LOG_BEGIN << ((XEntity*)currentTarget->Get())->ToString() << LOG_END;
		return YBehavior::NS_SUCCESS;
	}
	else
	{
		////LOG_BEGIN << "No Target" << LOG_END;
		return YBehavior::NS_FAILURE;
	}
}

bool GetTargetNameAction::OnLoaded(const pugi::xml_node& data)
{
	YB::PinCreation::CreatePin(this, m_Target, "Target", data, YBehavior::PinCreation::Flag::NoConst);
	if (!m_Target)
	{
		return false;
	}
	
	YBehavior::IPin* pTestVariable = nullptr;
	YB::PinCreation::CreatePin(this, pTestVariable, "TestVariable", data, YBehavior::PinCreation::Flag::None, YBehavior::Utility::GetCreateStr<YBehavior::INT>());

	return true;
}

bool ProjectVector3::OnLoaded(const pugi::xml_node& data)
{
	YBehavior::PinCreation::CreatePin(this, m_Input, "Input", data, YBehavior::PinCreation::Flag::NoConst);
	YBehavior::PinCreation::CreatePinIfExist(this, m_X, "X", data, YBehavior::PinCreation::Flag::IsOutput);
	YBehavior::PinCreation::CreatePinIfExist(this, m_Y, "Y", data, YBehavior::PinCreation::Flag::IsOutput);
	YBehavior::PinCreation::CreatePinIfExist(this, m_Z, "Z", data, YBehavior::PinCreation::Flag::IsOutput);

	return true;
}

YBehavior::NodeState ProjectVector3::Update(YBehavior::AgentPtr pAgent)
{
	YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Input, true);
	YBehavior::Vector3 v;
	m_Input->GetValue(pAgent->GetMemory(), v);
	if (m_X)
		m_X->SetValue(pAgent->GetMemory(), v.x);
	if (m_Y)
		m_Y->SetValue(pAgent->GetMemory(), v.y);
	if (m_Z)
		m_Z->SetValue(pAgent->GetMemory(), v.z);
	return YBehavior::NS_SUCCESS;
}

bool SetVector3::OnLoaded(const pugi::xml_node& data)
{
	YBehavior::PinCreation::CreatePin(this, m_Output, "Output", data, YBehavior::PinCreation::Flag::IsOutput);
	YBehavior::PinCreation::CreatePin(this, m_X, "X", data);
	YBehavior::PinCreation::CreatePin(this, m_Y, "Y", data);
	YBehavior::PinCreation::CreatePin(this, m_Z, "Z", data);

	return true;
}

YBehavior::NodeState SetVector3::Update(YBehavior::AgentPtr pAgent)
{
	YBehavior::Vector3 v;
	m_X->GetValue(pAgent->GetMemory(), v.x);
	m_Y->GetValue(pAgent->GetMemory(), v.y);
	m_Z->GetValue(pAgent->GetMemory(), v.z);
	m_Output->SetValue(pAgent->GetMemory(), v);
	YB_LOG_PIN_IF_HAS_DEBUG_POINT(m_Output, false);
	return YBehavior::NS_SUCCESS;
}
