#pragma once
#include "YBehavior/launcher.h"
#include "YBehavior/nodes/action.h"
#include "YBehavior/agent.h"

class XEntity;
class XAgent : public YBehavior::Agent
{
	XEntity* m_pEntity;
public:
	void SetEntity(XEntity* pEntity)
	{
		m_pEntity = pEntity;
	}
	XEntity* GetEntity() { return m_pEntity; }
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

