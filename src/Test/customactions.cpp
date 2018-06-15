#include "customactions.h"
#include "YBehavior/registerdata.h"

void MyLaunchCore::RegisterActions() const
{
	_Register<GetNameAction>();
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

