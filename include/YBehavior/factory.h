#ifndef _YBEHAVIOR_FACTORY_H_
#define _YBEHAVIOR_FACTORY_H_

#include "YBehavior/define.h"
#include "YBehavior/types.h"
#include <unordered_map>
#include <algorithm>
#ifdef YB_GCC
#include <cxxabi.h>
#endif

namespace YBehavior
{
#define REGISTER_TYPE(factoryPtr, typeName) factoryPtr->Create<typeName>()

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

		virtual T* Get(const STRING& name);

		template<typename finalType>
		void Create()
		{
#ifdef YB_MSVC
			std::string name(typeid(finalType).name());
#else
int status;
std::string name(abi::__cxa_demangle(typeid(finalType).name(), 0, 0, &status));
#endif
			int spacepos = (int)(name.find_last_of(" "));
			int pos = (int)(name.find_last_of("::"));

			m_ConstructorMap[name.substr(std::max(pos, spacepos) + 1)] = new TypeConstructor<finalType>();
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
