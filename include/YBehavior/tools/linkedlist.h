#ifndef _YBEHAVIOR_LINKEDLIST_H_
#define _YBEHAVIOR_LINKEDLIST_H_

#include <unordered_map>
#include "../logger.h"

namespace YBehavior
{
	template<typename T>
	class LinkedListNode
	{
	protected:
		T m_Value;
		LinkedListNode<T>* m_Pre;
		LinkedListNode<T>* m_Next;

	public:
		LinkedListNode()
			: m_Pre(nullptr)
			, m_Next(nullptr)
		{
		}

		LinkedListNode(const T& t)
			: m_Value(t)
			, m_Pre(nullptr)
			, m_Next(nullptr)
		{
		}

		LinkedListNode(T&& t)
			: m_Value(t)
			, m_Pre(nullptr)
			, m_Next(nullptr)
		{
		}

		inline LinkedListNode<T>* GetPre() { return m_Pre; }
		inline LinkedListNode<T>* GetNext() { return m_Next; }
		inline T& GetValue() { return m_Value; }

		void SetPre(LinkedListNode<T>* pre)
		{
			if (m_Pre != pre)
			{
				LinkedListNode<T>* oldPre = m_Pre;
				this->m_Pre = pre;

				if (pre != nullptr)
				{
					pre->SetNext(this);
				}
				if (oldPre != nullptr)
				{
					oldPre->m_Next = nullptr;
				}
			}
		}
		void SetNext(LinkedListNode<T>* next)
		{
			if (m_Next != next)
			{
				LinkedListNode<T>* oldNext = m_Next;
				this->m_Next = next;

				if (next != nullptr)
				{
					next->SetPre(this);
				}
				if (oldNext != nullptr)
				{
					oldNext->m_Pre = nullptr;
				}
			}
		}

		void InsertNext(LinkedListNode<T>* next)
		{
			if (next != m_Next)
			{
				LinkedListNode<T>* oldNext = m_Next;
				next->SetPre(this);
				next->SetNext(oldNext);
			}
		}

		void RemoveSelf()
		{
			if (m_Pre != nullptr)
				m_Pre->SetNext(m_Next);
			else if (m_Next != nullptr)
				m_Next->SetPre(m_Pre);
		}
	};

	template<typename T>
	class LinkedList : public LinkedListNode<T>
	{
	public:
		LinkedList()
			: LinkedListNode()
		{
		}

		~LinkedList()
		{
			LinkedListNode<T>* node;
			LinkedListNode<T>* next = GetNext();

			while (next != nullptr)
			{
				node = next->GetNext();
				delete next;
				next = node;
			}
		}

		LinkedListNode<T>* Append(const T& t)
		{
			LinkedListNode<T>* node = new LinkedListNode<T>(t);
			//LOG_BEGIN << "Insert node" << LOG_END;
			this->InsertNext(node);
			return node;
		}

		void Append(T&& t)
		{
			LinkedListNode<T>* node = new LinkedListNode<T>(t);
			this->InsertNext(node);
		}

		void Remove(LinkedListNode<T>* node)
		{
			//LOG_BEGIN << "Remove node" << LOG_END;
			if (node == nullptr)
				return;

			node->RemoveSelf();

			delete node;
		}
	};
}

#endif