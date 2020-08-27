#ifndef _YBEHAVIOR_RUNNINGCONTEXT_H_
#define _YBEHAVIOR_RUNNINGCONTEXT_H_

#include "YBehavior/types.h"
#include "behaviortree.h"

namespace YBehavior
{
	class RunningContext
	{
	protected:
		bool m_bRunningInCondition = false;
		UINT m_UID;
	public:
		inline void SetRunningInCondition(bool b) { m_bRunningInCondition = b; }
		inline bool IsRunningInCondition() const { return m_bRunningInCondition; }
		inline void SetUID(UINT uid) { m_UID = uid; }
		inline UINT GetUID() const { return m_UID; }
		void Reset();
		virtual ~RunningContext(){}
	protected:
		virtual void _OnReset() {};
	};

	class IContextCreator
	{
	public:
		virtual ~IContextCreator() {}
		virtual RunningContext* NewRC() const = 0;
		virtual void ReleaseRC(RunningContext* pRC) const = 0;
	};
	template<typename T>
	class ContextContainer : public IContextCreator
	{
	protected:
		T* m_RC;
	public:
		T * ConvertRC(BehaviorNodePtr node);
		T * CreateRC(BehaviorNodePtr node);
		inline T* GetRC() { return m_RC; }
		RunningContext* NewRC() const override;
		void ReleaseRC(RunningContext* pRC) const override;
	};

	template<typename T>
	RunningContext* YBehavior::ContextContainer<T>::NewRC() const
	{
		return ObjectPool<T>::Get();
	}

	template<typename T>
	void YBehavior::ContextContainer<T>::ReleaseRC(RunningContext* pRC) const
	{
		ObjectPool<T>::Recycle(static_cast<T*>(pRC));
	}

	template<typename T>
	T * YBehavior::ContextContainer<T>::CreateRC(BehaviorNodePtr node)
	{
		if (!ConvertRC(node))
		{
			node->TryCreateRC();
			m_RC = (T*)node->GetRC();
		}
		return m_RC;
	}

	template<typename T>
	T * YBehavior::ContextContainer<T>::ConvertRC(BehaviorNodePtr node)
	{
		m_RC = nullptr;
		if (node->GetRC())
		{
			if (node->GetRC()->GetUID() == node->GetUID())
				m_RC = static_cast<T*>(node->GetRC());
			else
			{
				ERROR_BEGIN << "RunningContext not match with Node " << Utility::ToString(node->GetUID()) << ERROR_END;
				return nullptr;
			}
		}
		return m_RC;
	}

	class VectorTraversalContext : public RunningContext
	{
	public:
		int Current;

	protected:
		void _OnReset() override;
	};

	class RandomVectorTraversalContext : public VectorTraversalContext
	{
	public:
		StdVector<int> IndexList;

	protected:
		void _OnReset() override;
	};
}

#endif