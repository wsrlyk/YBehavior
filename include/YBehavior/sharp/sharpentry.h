#pragma once
#include "Ybehavior/agent.h"
#include "YBehavior/sharp/sharpnode.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/interface.h"

extern "C" YBEHAVIOR_API YBehavior::Entity* CreateEntity()
{
	return new YBehavior::Entity();
}

extern "C" YBEHAVIOR_API void DeleteEntity(YBehavior::Entity* pObject)
{
	if (pObject != NULL)
	{
		delete pObject;
		pObject = NULL;
	}
}
extern "C" YBEHAVIOR_API YBehavior::Agent* CreateAgent(YBehavior::Entity* pEntity)
{
	return new YBehavior::Agent(pEntity);
}

extern "C" YBEHAVIOR_API void DeleteAgent(YBehavior::Agent* pObject)
{
	if (pObject != NULL)
	{
		delete pObject;
		pObject = NULL;
	}
}

extern "C" YBEHAVIOR_API void RegisterSharpNode(
	YBehavior::CSTRING name,
	YBehavior::OnSharpNodeLoadedDelegate onload,
	YBehavior::OnSharpNodeUpdateDelegate onupdate)
{
	YBehavior::NodeFactory::Instance()->SetSharpCallback(name, onload, onupdate);
}

extern "C" YBEHAVIOR_API void RegisterLoadData(YBehavior::LoadDataDelegate loaddata)
{
	YBehavior::TreeMgr::Instance()->SetLoadDataCallback(loaddata);
}

extern "C" YBEHAVIOR_API void SetTree(YBehavior::Agent* pAgent, YBehavior::CSTRING treeName)
{
	pAgent->SetTree(treeName);
}

extern "C" YBEHAVIOR_API void Tick(YBehavior::Agent* pAgent)
{
	pAgent->Tick();
}

extern "C" YBEHAVIOR_API YBehavior::ISharedVariableEx* CreateVariable(
	YBehavior::BehaviorNode* pNode,
	YBehavior::CSTRING attrName,
	const pugi::xml_node* data,
	bool bSingle,
	char variableType)
{
	if (pNode != nullptr)
	{
		YBehavior::ISharedVariableEx* v;
		YBehavior::TYPEID res = pNode->CreateVariable(v, attrName, *data, bSingle, variableType);
		return v;
	}
	return nullptr;
}
