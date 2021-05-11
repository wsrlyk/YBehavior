#pragma once
#include "YBehavior/launcher.h"
#include "YBehavior/agent.h"
#include "YBehavior/shareddataex.h"

class XEntity;
class XAgent : public YBehavior::Agent
{
	static YBehavior::KEY tickCount0;
	static YBehavior::KEY tickCount1;
	static YBehavior::KEY isfighting;
	static YBehavior::KEY heartrate;
	static YBehavior::KEY isdead;

public:
	XAgent(YBehavior::Entity* pEntity)
		: Agent(pEntity)
	{

	}
	static void InitData()
	{
		//YBehavior::TreeKeyMgr::Instance()->SetActiveTree(nullptr, true);
		tickCount0 = YBehavior::TreeKeyMgr::Instance()->CreateKeyByName<YBehavior::INT>("tickCount0");
		tickCount1 = YBehavior::TreeKeyMgr::Instance()->CreateKeyByName<YBehavior::INT>("tickCount1");
		isfighting = YBehavior::TreeKeyMgr::Instance()->CreateKeyByName<YBehavior::BOOL>("isfighting");
		heartrate = YBehavior::TreeKeyMgr::Instance()->CreateKeyByName<YBehavior::FLOAT>("heartrate");
		isdead = YBehavior::TreeKeyMgr::Instance()->CreateKeyByName<YBehavior::BOOL>("isdead");
	}

	void SetData()
	{
		//this->GetSharedData()->Set<YBehavior::INT>(tickCount0, 3);
		//this->GetSharedData()->Set<YBehavior::BOOL>(isfighting, YBehavior::Utility::FALSE_VALUE);
		//this->GetSharedData()->Set<YBehavior::FLOAT>(heartrate, 2);

		/*YBehavior::KEY f = */YBehavior::TreeKeyMgr::Instance()->GetKeyByName<YBehavior::FLOAT>("f");
		fftest = YBehavior::TreeKeyMgr::Instance()->GetKeyByName<YBehavior::VecFloat>("fftest");
	}

	void Update();
protected:
	YBehavior::KEY fftest;
};

class XEntity : public YBehavior::Entity
{
	XAgent* pAgent;
	std::string m_Name;
public:
	XEntity(const YBehavior::STRING& name, const YBehavior::STRING& machineName, const StdVector<YBehavior::STRING>* trees = nullptr, const StdVector<YBehavior::STRING>* subs = nullptr)
	{
		m_Name = name;
		pAgent = new XAgent(this);
		YBehavior::BehaviorKey key(machineName, trees, subs);
		pAgent->SetBehavior(key);
		pAgent->SetData();
	}

	~XEntity()
	{
		delete pAgent;
	}
	inline const std::string& GetName() const { return m_Name; }
	inline XAgent* GetAgent() { return pAgent; }
	YBehavior::STRING ToString() const override;
};

class Action : public YBehavior::LeafNode<>
{

};

class GetNameAction : public Action
{
public:
	TREENODE_DEFINE(GetNameAction)
protected:
	virtual YBehavior::NodeState Update(YBehavior::AgentPtr pAgent);
};

class GetTargetNameAction : public Action
{
public:
	TREENODE_DEFINE(GetTargetNameAction)
protected:
	YBehavior::NodeState Update(YBehavior::AgentPtr pAgent) override;
	bool OnLoaded(const pugi::xml_node& data) override;

	YBehavior::SharedVariableEx<YBehavior::EntityWrapper>* m_Target;
};

class SelectTargetAction : public Action
{
public:
	TREENODE_DEFINE(SelectTargetAction)

protected:
	YBehavior::NodeState Update(YBehavior::AgentPtr pAgent) override;
	bool OnLoaded(const pugi::xml_node& data) override;

	YBehavior::SharedVariableEx<YBehavior::EntityWrapper>* m_Target;
};


class MyLaunchCore : public YBehavior::LaunchCore
{
public:
	virtual void RegisterActions() const;
	int StartWithDebugListeningPort() const override { return 444; }
};

