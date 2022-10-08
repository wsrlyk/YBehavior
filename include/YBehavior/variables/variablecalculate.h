#ifndef _YBEHAVIOR_VARIABLECALCULATE_H_
#define _YBEHAVIOR_VARIABLECALCULATE_H_

#include "YBehavior/interface.h"
#include "YBehavior/types.h"
#include "YBehavior/singleton.h"
#include <map>
#include "YBehavior/logger.h"

namespace YBehavior
{
	enum struct CalculateType
	{
		ADD,
		SUB,
		MUL,
		DIV,
	};

	class IVariableCalculateHelper
	{
	public:
		virtual ~IVariableCalculateHelper() {}
	public:
		virtual void Calculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op) const = 0;
		virtual const void* Calculate(const void* pRight0, const void* pRight1, CalculateType op) const = 0;
		virtual void Calculate(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, CalculateType op) const = 0;
	};

	//struct NormalCalculateTag {};
	//struct InvalidCalculateTag {};
	//template<typename T> struct CalculateTagDispatchTrait {
	//	using Tag = NormalCalculateTag;
	//};
	//template<> struct CalculateTagDispatchTrait<EntityWrapper> {
	//	using Tag = InvalidCalculateTag;
	//};

	class ValueCalculate
	{
	public:
		template<typename T>
		static void Calculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op)
		{
			return _DoCalculate<T>(pLeft, pRight0, pRight1, op/*, typename CalculateTagDispatchTrait<T>::Tag{}*/);
		}
		template<typename TL, typename TR0, typename TR1>
		static void Calculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op)
		{
			return _DoCalculate<TL, TR0, TR1>(pLeft, pRight0, pRight1, op/*, typename CalculateTagDispatchTrait<T>::Tag{}*/);
		}

	private:
		template<typename T>
		static void _DoCalculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/);
		//template<typename T>
		//static void _DoCalculate(const void* pFrom, void* pTo, InvalidCalculateTag) {}
		template<typename TL, typename TR0, typename TR1>
		static void _DoCalculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/) {}

		template<typename T>
		static void _Add(const T& left, const T& right, T& output);
		template<typename T>
		static void _Sub(const T& left, const T& right, T& output);
		template<typename T>
		static void _Mul(const T& left, const T& right, T& output);
		template<typename T>
		static void _Div(const T& left, const T& right, T& output);
	};

	template<typename T>
	void ValueCalculate::_DoCalculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/)
	{
		if (pLeft == nullptr)
		{
			ERROR_BEGIN << "pLeft is null in Calculate" << ERROR_END;
			return;
		}
		if (pRight0 == nullptr)
		{
			ERROR_BEGIN << "pRight0 is null in Calculate" << ERROR_END;
			return;
		}
		if (pRight1 == nullptr)
		{
			ERROR_BEGIN << "pRight1 is null in Calculate" << ERROR_END;
			return;
		}

		const T& right0 = *((const T*)pRight0);
		const T& right1 = *((const T*)pRight1);

		T& left = *((T*)pLeft);
		switch (op)
		{
		case YBehavior::CalculateType::ADD:
			_Add(right0, right1, left);
			break;
		case YBehavior::CalculateType::SUB:
			_Sub(right0, right1, left);
			break;
		case YBehavior::CalculateType::MUL:
			_Mul(right0, right1, left);
			break;
		case YBehavior::CalculateType::DIV:
			_Div(right0, right1, left);
			break;
		default:
			return;
		}
	}

	template<>
	void ValueCalculate::_DoCalculate<STRING>(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/);
	template<>
	void ValueCalculate::_DoCalculate<Vector3, Vector3, float>(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op/*, NormalCalculateTag*/);

	template<typename T>
	void ValueCalculate::_Add(const T& left, const T& right, T& output)
	{
		output = left + right;
	}
	template<typename T>
	void ValueCalculate::_Sub(const T& left, const T& right, T& output)
	{
		output = left - right;
	}
	template<typename T>
	void ValueCalculate::_Mul(const T& left, const T& right, T& output)
	{
		output = left * right;
	}
	template<typename T>
	void ValueCalculate::_Div(const T& left, const T& right, T& output)
	{
		output = left / right;
	}
	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename TL, typename ...TR>
	class VariableCalculateHelper : public IVariableCalculateHelper
	{
		static VariableCalculateHelper<TL, TR...> s_Instance;
	public:
		static IVariableCalculateHelper* Get() { return &s_Instance; }

		void Calculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op) const override
		{
			ValueCalculate::Calculate<TL, TR...>(pLeft, pRight0, pRight1, op);
		}
		const void* Calculate(const void* pRight0, const void* pRight1, CalculateType op) const override
		{
			TL& left = PreservedValue<TL>::Data;
			ValueCalculate::Calculate<TL, TR...>(&left, pRight0, pRight1, op);
			return &left;
		}
		void Calculate(IMemory* pMemory, ISharedVariableEx* pLeft, ISharedVariableEx* pRight0, ISharedVariableEx* pRight1, CalculateType op) const override
		{
			TL left;
			ValueCalculate::Calculate<TL, TR...>(&left, pRight0->GetValue(pMemory), pRight1->GetValue(pMemory), op);
			pLeft->SetValue(pMemory, &left);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class VariableCalculateMgr : public Singleton<VariableCalculateMgr>
	{
		/// <summary>
		/// Left, Right0, Right1
		/// </summary>
		std::map<std::tuple<TYPEID, TYPEID, TYPEID>, const IVariableCalculateHelper*> m_Calculates;
	public:
		VariableCalculateMgr();
		~VariableCalculateMgr();

		const IVariableCalculateHelper* Get(TYPEID tl, TYPEID tr0, TYPEID tr1) const;
		const IVariableCalculateHelper* Get(TYPEID t) const { return Get(t, t, t); }

		template<typename T>
		const IVariableCalculateHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
		template<typename TL, typename TR0, typename TR1>
		const IVariableCalculateHelper* Get() const
		{
			return Get(GetTypeID<TL>(), GetTypeID<TR0>(), GetTypeID<TR0>());
		}
	};
}

#endif
