#pragma once
#include "YBehavior/launcher.h"
#include "YBehavior/nodes/action.h"
#include "YBehavior/agent.h"
#include "YBehavior/shareddataex.h"

class XEntity;
class XAgent : public YBehavior::Agent
{
	XEntity* m_pEntity;

	static YBehavior::KEY tickCount0;
	static YBehavior::KEY tickCount1;
	static YBehavior::KEY isfighting;
	static YBehavior::KEY heartrate;
	static YBehavior::KEY isdead;

public:
	void SetEntity(XEntity* pEntity)
	{
		m_pEntity = pEntity;
	}
	static void InitData()
	{
		YBehavior::NodeFactory::Instance()->SetActiveTree("");
		tickCount0 = YBehavior::NodeFactory::Instance()->CreateKeyByName<YBehavior::INT>("tickCount0");
		tickCount1 = YBehavior::NodeFactory::Instance()->CreateKeyByName<YBehavior::INT>("tickCount1");
		isfighting = YBehavior::NodeFactory::Instance()->CreateKeyByName<YBehavior::BOOL>("isfighting");
		heartrate = YBehavior::NodeFactory::Instance()->CreateKeyByName<YBehavior::FLOAT>("heartrate");
		isdead = YBehavior::NodeFactory::Instance()->CreateKeyByName<YBehavior::BOOL>("isdead");
	}

	void SetData()
	{
		this->GetSharedData()->Set<YBehavior::INT>(tickCount0, 3);
		this->GetSharedData()->Set<YBehavior::BOOL>(isfighting, YBehavior::FALSE);
		this->GetSharedData()->Set<YBehavior::FLOAT>(heartrate, 2);
	}
	XEntity* GetEntity() { return m_pEntity; }
	void Update();
};
class XEntity
{
	XAgent* pAgent;
	std::string m_Name;
public:
	XEntity(const std::string& name)
	{
		m_Name = name;
		pAgent = new XAgent();
		pAgent->SetTree("Monster_BlackCrystal2");
		pAgent->SetData();
	}
	~XEntity()
	{
		delete pAgent;
	}
	inline const std::string& GetName() { return m_Name; }
	inline XAgent* GetAgent() { return pAgent; }
};


class GetNameAction : public YBehavior::Action
{
public:
	YBehavior::STRING GetClassName() const override { return "GetNameAction"; }
protected:
	virtual YBehavior::NodeState Update(YBehavior::AgentPtr pAgent);
};


class MyLaunchCore : public YBehavior::LaunchCore
{
public:
	virtual void RegisterActions() const;
};

