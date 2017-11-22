#include "YBehavior/agent.h"
#include "YBehavior/behaviortreemgr.h"

using namespace YBehavior;
class XEntity
{
	YBehavior::Agent* pAgent;

public:
	XEntity()
	{
		pAgent = new YBehavior::Agent();
	}

	inline Agent* GetAgent() { return pAgent; }
};

int main(int argc, char* argv)
{
	std::vector<int> a(1);
	XEntity* pEntity = new XEntity();
	//pEntity->GetAgent()->GetSharedData()->GetBool(3);

	float b = YBehavior::SharedData::s_DefaultFloat;
	SharedInt aa;
	//aa.GetValue(pEntity->GetAgent()->GetSharedData());
	TreeMgr::Instance()->LoadOneTree("Monster_BlackCrystal");

	return 0;
}