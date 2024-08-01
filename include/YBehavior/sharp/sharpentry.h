#ifdef YSHARP
#pragma once
#include "YBehavior/agent.h"
#include "YBehavior/sharp/sharpnode.h"
#include "YBehavior/nodefactory.h"
#include "YBehavior/behaviortreemgr.h"
#include "YBehavior/interface.h"
#include "YBehavior/mgrs.h"
#include "YBehavior/fsm/machinemgr.h"
#include "YBehavior/sharp/sharplaunch.h"
#include "YBehavior/behaviortree.h"
#include "YBehavior/sharedvariablecreatehelper.h"
#include "YBehavior/variablecreation.h"
#include "YBehavior/behaviorprocess.h"
#include "YBehavior/sharp/sharpentry_buffer.h"
#include "YBehavior/sharp/sharputility.h"
#include "YBehavior/sharp/sharpnode.h"
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

	YBehavior::Mgrs::Instance()->Reset();
}

extern "C" YBEHAVIOR_API void RegisterSharpNode(
	YBehavior::CSTRING_CONST name,
	YBehavior::OnSharpNodeLoadedDelegate onload,
	YBehavior::OnSharpNodeUpdateDelegate onupdate,
	int index)
{
	YBehavior::NodeFactory::Instance()->RegisterSharpNode(name, index);
}
extern "C" YBEHAVIOR_API void RegisterSharpNodeCallback(
	YBehavior::OnSharpNodeLoadedDelegate onload,
	YBehavior::OnSharpNodeUpdateDelegate onupdate
	)
{
	YBehavior::SharpNode::SetCallback(onload, onupdate);
}

extern "C" YBEHAVIOR_API void RegisterGetFilePathCallback(YBehavior::SharpGetFilePathDelegate callback)
{
	YBehavior::SharpUtility::SetGetFilePathCallback(callback);
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
	YBehavior::TreeNode* pNode,
	YBehavior::CSTRING_CONST attrName,
	const pugi::xml_node* data,
	bool noConst)
{
	if (pNode != nullptr)
	{
		YBehavior::ISharedVariableEx* v;
		auto res = YBehavior::VariableCreation::CreateVariableIfExist(pNode, v, attrName, *data, noConst);
		return v;
	}
	return nullptr;
}

extern "C" YBEHAVIOR_API bool TryGetValue(
	YBehavior::TreeNode* pNode,
	YBehavior::CSTRING_CONST attrName,
	const pugi::xml_node* data)
{
	if (pNode != nullptr)
	{
		YBehavior::STRING s;
		if (YBehavior::VariableCreation::TryGetValue(pNode, attrName, *data, s))
		{
			YBehavior::SharpBuffer::Set(&s, YBehavior::GetTypeID<YBehavior::STRING>());
			return true;
		}
		return false;
	}
	return nullptr;
}


extern "C" YBEHAVIOR_API void SetSharedDataByString(YBehavior::Agent* pAgent, YBehavior::CSTRING name, YBehavior::CSTRING value, YBehavior::CHAR separator)
{
	auto maps = YBehavior::SharedVariableCreateHelperMgr::GetAllHelpers();
	for (int i = 0; i < MAX_TYPE_KEY; ++i)
	{
		if (maps[i]->TrySetSharedData(pAgent->GetMemory()->GetMainData(), name, value, separator))
		{
			break;
		}
	}
}

extern "C" YBEHAVIOR_API void LogVariable(YBehavior::TreeNode* pNode, YBehavior::ISharedVariableEx* pVariable, bool before)
{
#ifdef YDEBUGGER
	if (YB::TreeNodeContext::HasDebugPoint(pNode->GetDebugHelper()))
		YB::TreeNodeContext::LogVariable(pNode->GetDebugHelper(), pVariable, before);
#else
#endif
}


extern "C" YBEHAVIOR_API bool HasDebugPoint(YBehavior::TreeNode* pNode)
{
#ifdef YDEBUGGER
	return YB::TreeNodeContext::HasDebugPoint(pNode->GetDebugHelper());
#else
	return false;
#endif
}
extern "C" YBEHAVIOR_API void LogInfo(YBehavior::TreeNode* pNode, YBehavior::CSTRING_CONST str)
{
#ifdef YDEBUGGER
	if (YB::TreeNodeContext::HasDebugPoint(pNode->GetDebugHelper()))
		YB::TreeNodeContext::GetLogInfo(pNode->GetDebugHelper()) << str;
#else
	
#endif
}

#endif