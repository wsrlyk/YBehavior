#ifdef SHARP
#pragma once
#include "Ybehavior/agent.h"
#include "YBehavior/sharp/sharpnode.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/interface.h"
#include "YBehavior/mgrs.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/sharp/sharplaunch.h"

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
extern "C" YBEHAVIOR_API void InitSharp(int debugPort)
{
	YBehavior::SharpLaunchCore core(debugPort);
	YBehavior::Launcher::Launch(core);
}

extern "C" YBEHAVIOR_API void RegisterSharpNode(
	YBehavior::CSTRING_CONST name,
	YBehavior::OnSharpNodeLoadedDelegate onload,
	YBehavior::OnSharpNodeUpdateDelegate onupdate)
{
	YBehavior::NodeFactory::Instance()->SetSharpCallback(name, onload, onupdate);
}

extern "C" YBEHAVIOR_API void RegisterLoadData(YBehavior::LoadDataDelegate loaddata)
{
	YBehavior::Mgrs::Instance()->GetTreeMgr()->SetLoadDataCallback(loaddata);
	YBehavior::Mgrs::Instance()->GetMachineMgr()->SetLoadDataCallback(loaddata);
	//YBehavior::TreeMgr::Instance()->SetLoadDataCallback(loaddata);
}

extern "C" YBEHAVIOR_API void RegisterLogCallback(
	YBehavior::SharpLogDelegate log,
	YBehavior::SharpLogDelegate error,
	YBehavior::SharpLogDelegate threadlog,
	YBehavior::SharpLogDelegate threaderror)
{
	YBehavior::SharpLaunchCore::SetCallback(log, error, threadlog, threaderror);
}

extern "C" YBEHAVIOR_API bool SetBehavior(
	YBehavior::Agent* pAgent, 
	YBehavior::CSTRING_CONST fsmName,
	YBehavior::CSTRING_CONST* state2Tree, YBehavior::UINT stSize,
	YBehavior::CSTRING_CONST* tree2Tree, YBehavior::UINT ttSize
	)
{
	StdVector<YBehavior::STRING> s2t;
	StdVector<YBehavior::STRING> *pS2t = nullptr;
	if (stSize > 0 && state2Tree != nullptr && stSize % 2 == 0)
	{
		for (YBehavior::UINT i = 0; i < stSize; i += 2)
		{
			s2t.emplace_back(state2Tree[i]);
			s2t.emplace_back(state2Tree[i + 1]);
		}
		pS2t = &s2t;
	}
	StdVector<YBehavior::STRING> t2t;
	StdVector<YBehavior::STRING> *pT2t = nullptr;
	if (ttSize > 0 && tree2Tree != nullptr && ttSize % 2 == 0)
	{
		for (YBehavior::UINT i = 0; i < ttSize; i += 2)
		{
			t2t.emplace_back(tree2Tree[i]);
			t2t.emplace_back(tree2Tree[i + 1]);
		}
		pT2t = &t2t;
	}
	YBehavior::BehaviorKey key(fsmName, pS2t, pT2t);
	return pAgent->SetBehavior(key);
}

extern "C" YBEHAVIOR_API void Tick(YBehavior::Agent* pAgent)
{
	pAgent->Tick();
}

extern "C" YBEHAVIOR_API YBehavior::ISharedVariableEx* CreateVariable(
	YBehavior::BehaviorNode* pNode,
	YBehavior::CSTRING_CONST attrName,
	const pugi::xml_node* data,
	char variableType)
{
	if (pNode != nullptr)
	{
		YBehavior::ISharedVariableEx* v;
		YBehavior::TYPEID res = pNode->CreateVariable(v, attrName, *data, variableType);
		return v;
	}
	return nullptr;
}

#endif