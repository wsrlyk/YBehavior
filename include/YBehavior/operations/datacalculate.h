#pragma once
#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/logger.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	enum struct CalculateType
	{
		ADD,
		SUB,
		MUL,
		DIV,
	};

	class IDataCalculateHelper
	{
	public:
		virtual ~IDataCalculateHelper() {}
	public:
		virtual void Calculate(void* pLeft, const void* pRight0, const void* pRight1, CalculateType op) const = 0;
		virtual const void* Calculate(const void* pRight0, const void* pRight1, CalculateType op) const = 0;
		virtual void Calculate(IMemory* pMemory, IPin* pLeft, IPin* pRight0, IPin* pRight1, CalculateType op) const = 0;
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
	class DataCalculateHelper : public IDataCalculateHelper
	{
		static DataCalculateHelper<TL, TR...> s_Instance;
	public:
		static IDataCalculateHelper* Get() { return &s_Instance; }

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
		void Calculate(IMemory* pMemory, IPin* pLeft, IPin* pRight0, IPin* pRight1, CalculateType op) const override
		{
			TL left;
			ValueCalculate::Calculate<TL, TR...>(&left, pRight0->GetValuePtr(pMemory), pRight1->GetValuePtr(pMemory), op);
			pLeft->SetValue(pMemory, &left);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class DataCalculateMgr : public Singleton<DataCalculateMgr>
	{
		/// <summary>
		/// Left, Right0, Right1
		/// </summary>
		small_map<std::tuple<TYPEID, TYPEID, TYPEID>, const IDataCalculateHelper*> m_Calculates;
	public:
		DataCalculateMgr();
		~DataCalculateMgr();

		const IDataCalculateHelper* Get(TYPEID tl, TYPEID tr0, TYPEID tr1) const;
		const IDataCalculateHelper* Get(TYPEID t) const { return Get(t, t, t); }

		template<typename T>
		const IDataCalculateHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
		template<typename TL, typename TR0, typename TR1>
		const IDataCalculateHelper* Get() const
		{
			return Get(GetTypeID<TL>(), GetTypeID<TR0>(), GetTypeID<TR0>());
		}
	};
}
