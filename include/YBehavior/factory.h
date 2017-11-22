#ifndef _YBEHAVIOR_FACTORY_H_
#define _YBEHAVIOR_FACTORY_H_

#include "YBehavior/define.h"
#include "YBehavior/shareddata.h"
#include <unordered_map>

namespace YBehavior
{
#define REGISTER_TYPE(factoryPtr, typeName) factoryPtr->Create<typeName>(#typeName)

	template<typename T>
	class Factory
	{
	public:
		class IConstructor
		{
		public:
			virtual ~IConstructor() {}
			virtual T* Create() = 0;
		};

		template<typename finalType>
		class TypeConstructor : public Factory<T>::IConstructor
		{
			virtual T* Create() { return new finalType; }
		};

		T* Get(const STRING& name);

		template<typename finalType>
		void Create(const STRING& name)
		{
			m_ConstructorMap[name] = new TypeConstructor<finalType>();
		}

	private:
		std::unordered_map<STRING, IConstructor*> m_ConstructorMap;

	public:
		~Factory()
		{
			for (auto it = m_ConstructorMap.begin(); it != m_ConstructorMap.end(); ++it)
			{
				delete it->second;
			}
			m_ConstructorMap.clear();
		}
	};

	template<typename T>
	T* Factory<T>::Get(const STRING& name)
	{
		auto it = m_ConstructorMap.find(name);
		if (it == m_ConstructorMap.end())
			return nullptr;
		return it->second->Create();
	}
}

#endif