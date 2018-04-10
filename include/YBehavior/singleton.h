#ifndef _YBEHAVIOR_SINGLETON_H_
#define _YBEHAVIOR_SINGLETON_H_

namespace YBehavior
{
	template <typename T>
	class Singleton
	{
		static T* s_Instance;
	public:
		static T* Instance()
		{
			if (s_Instance == nullptr)
				s_Instance = new T();
			return s_Instance;
		}
	};

	template <typename T>
	T* Singleton<T>::s_Instance = nullptr;
}

#endif