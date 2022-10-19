#ifndef _YBEHAVIOR_VARIABLECONVERT_H_
#define _YBEHAVIOR_VARIABLECONVERT_H_

#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/utility.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	class IVariableConvertHelper
	{
	public:
		virtual ~IVariableConvertHelper() {}
	public:
		virtual bool Convert(const void* pFrom, void* pTo) const = 0;
		virtual bool Convert(IMemory* pMemory, ISharedVariableEx* pFrom, ISharedVariableEx* pTo) const = 0;
	};

	struct NormalConvertTag {};
	struct ToStringConvertTag {};
	struct FromStringConvertTag {};
	struct InvalidConvertTag {};
	template<typename From, typename To> struct ConvertTagDispatchTrait {
		using Tag = NormalConvertTag;
	};
	template<typename From> struct ConvertTagDispatchTrait<From, String> {
		using Tag = ToStringConvertTag;
	};
	template<typename To> struct ConvertTagDispatchTrait<String, To> {
		using Tag = FromStringConvertTag;
	};
	template<typename From> struct ConvertTagDispatchTrait<From, EntityWrapper> {
		using Tag = InvalidConvertTag;
	};
	template<typename To> struct ConvertTagDispatchTrait<EntityWrapper, To> {
		using Tag = InvalidConvertTag;
	};

	class ValueConvert
	{
	public:
		template<typename FromType, typename ToType>
		static bool Convert(const void* pFrom, void* pTo)
		{
			return _DoConvert<FromType, ToType>(pFrom, pTo, typename ConvertTagDispatchTrait<FromType, ToType>::Tag{});
		}
	private:
		template<typename FromType, typename ToType>
		static bool _DoConvert(const void* pFrom, void* pTo, NormalConvertTag);
		template<typename FromType, typename ToType>
		static bool _DoConvert(const void* pFrom, void* pTo, ToStringConvertTag);
		template<typename FromType, typename ToType>
		static bool _DoConvert(const void* pFrom, void* pTo, FromStringConvertTag);
		template<typename FromType, typename ToType>
		static bool _DoConvert(const void* pFrom, void* pTo, InvalidConvertTag) { return false; }
	};


	template<typename FromType, typename ToType>
	inline bool ValueConvert::_DoConvert(const void* pFrom, void* pTo, NormalConvertTag)
	{
		const FromType& from = *((const FromType*)pFrom);
		ToType& to = *((ToType*)pTo);
		to = (ToType)from;
		return true;
	}

	template<typename FromType, typename ToType>
	inline bool ValueConvert::_DoConvert(const void* pFrom, void* pTo, ToStringConvertTag)
	{
		const FromType& from = *((const FromType*)pFrom);
		STRING& to = *((STRING*)pTo);
		to = Utility::ToString(from);
		return true;
	}

	template<typename FromType, typename ToType>
	inline bool ValueConvert::_DoConvert(const void* pFrom, void* pTo, FromStringConvertTag)
	{
		const STRING& from = *((const STRING*)pFrom);
		ToType& to = *((ToType*)pTo);
		to = Utility::ToType<ToType>(from);
		return true;
	}

	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename FromType, typename ToType>
	class VariableConvertHelper : public IVariableConvertHelper
	{
		static VariableConvertHelper<FromType, ToType> s_Instance;
	public:
		static IVariableConvertHelper* Get() { return &s_Instance; }

		bool Convert(const void* pFrom, void* pTo) const override
		{
			return ValueConvert::Convert<FromType, ToType>(pFrom, pTo);
		}
		bool Convert(IMemory* pMemory, ISharedVariableEx* pFrom, ISharedVariableEx* pTo) const override
		{
			return ValueConvert::Convert<FromType, ToType>(pFrom->GetValue(pMemory), const_cast<void*>(pTo->GetValue(pMemory)));
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class VariableConvertMgr : public Singleton<VariableConvertMgr>
	{
		small_map<std::pair<TYPEID, TYPEID>, const IVariableConvertHelper*> m_Converts;
	public:
		VariableConvertMgr();
		~VariableConvertMgr();

		const IVariableConvertHelper* GetConvert(TYPEID from, TYPEID to) const;

		template<typename FromType, typename ToType>
		const IVariableConvertHelper* GetConvert() const
		{
			return GetConvert(GetTypeID<FromType>(), GetTypeID<ToType>());
		}
	};
}

#endif
