#ifndef _YBEHAVIOR_BEHAVIORTREE_H_
#define _YBEHAVIOR_BEHAVIORTREE_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddata.h"
#include "YBehavior/factory.h"
#include <unordered_map>
namespace pugi
{
	class xml_node;
}

namespace YBehavior
{
	class NodeFactory;
	class YBEHAVIOR_API BehaviorNode
	{
	protected:
		typedef BehaviorNode* BehaviorNodePtr;
		BehaviorNodePtr m_Parent;
		std::vector<BehaviorNodePtr>* m_Childs;

	public:
		BehaviorNode();
		virtual ~BehaviorNode();

		inline BehaviorNodePtr GetParent() { return m_Parent;}
		BehaviorNodePtr GetChild(UINT index);
		void AddChild(BehaviorNode* child);
		virtual void OnLoaded(const pugi::xml_node& data) {}

		static BehaviorNode* CreateNodeByName(const STRING& name);
		static NodeFactory* GetNodeFactory() { return s_NodeFactory; }
	protected:
		void _DestroyChilds();

		static NodeFactory* s_NodeFactory;
	};

	class YBEHAVIOR_API NodeFactory: public Factory<BehaviorNode>
	{
	public:
		NodeFactory();
		void SetActiveTree(const STRING& tree);
		INT CreateIndexByName(const STRING& name);
	public:
		struct NameIndexInfo
		{
			std::unordered_map<STRING, INT> mNameHash;
			INT mNameIndex;

			void Reset()
			{
				mNameHash.clear();
				mNameIndex = 0;
			}
		};
	private:
		//static std::map<std::string, NameIndexInfo> mNameIndexInfos;
		NameIndexInfo mCommonNameIndexInfo;
		NameIndexInfo mTempNameIndexInfo;
		NameIndexInfo* mpCurActiveNameIndexInfo;
		STRING mCurActiveTreeName;
	};

	class YBEHAVIOR_API BehaviorTree : public BehaviorNode
	{
	private:
		SharedData* m_SharedData;	///> 原始数据，每个使用此树的Agent都从这拷数据作为初始化
	public:
		BehaviorTree();
		~BehaviorTree();
		virtual void OnLoaded(const pugi::xml_node& data);
	};
}

#endif