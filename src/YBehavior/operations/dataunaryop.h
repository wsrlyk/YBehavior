#pragma once
#include "YBehavior/interface.h"
#include "YBehavior/types/types.h"
#include "YBehavior/singleton.h"
#include "YBehavior/logger.h"
#include "YBehavior/types/smallmap.h"

namespace YBehavior
{
	enum struct UnaryOpType
	{
		ABS,
	};

	class IDataUnaryOpHelper
	{
	public:
		virtual ~IDataUnaryOpHelper() {}
	public:
		virtual void Operate(void* pOutput, const void* pInput, UnaryOpType op) const = 0;
		virtual const void* Operate(const void* pInput, UnaryOpType op) const = 0;
		virtual void Operate(IMemory* pMemory, IPin* pOutput, IPin* pInput, UnaryOpType op) const = 0;
	};

	class ValueUnaryOp
	{
	public:
		template<typename T>
		static void Operate(void* pOutput, const void* pInput, UnaryOpType op)
		{
			return _DoOperate<T>(pOutput, pInput, op);
		}

	private:
		template<typename T>
		static void _DoOperate(void* pOutput, const void* pInput, UnaryOpType op);

		template<typename T>
		static void _Abs(const T& input,  T& output);
	};

	template<typename T>
	void ValueUnaryOp::_DoOperate(void* pOutput, const void* pInput, UnaryOpType op)
	{
		if (pOutput == nullptr)
		{
			ERROR_BEGIN << "pOutput is null in UnaryOp" << ERROR_END;
			return;
		}
		if (pInput == nullptr)
		{
			ERROR_BEGIN << "pInput is null in UnaryOp" << ERROR_END;
			return;
		}

		const T& input = *((const T*)pInput);

		T& output = *((T*)pOutput);
		switch (op)
		{
		case YBehavior::UnaryOpType::ABS:
			_Abs(input, output);
			break;
		default:
			return;
		}
	}

	template<typename T>
	void ValueUnaryOp::_Abs(const T& input, T& output)
	{
		output = std::abs(input);
	}
	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////

	template<typename T>
	class DataUnaryOpHelper : public IDataUnaryOpHelper
	{
		static DataUnaryOpHelper<T> s_Instance;
	public:
		static IDataUnaryOpHelper* Get() { return &s_Instance; }

		void Operate(void* pOutput, const void* pInput, UnaryOpType op) const override
		{
			ValueUnaryOp::Operate<T>(pOutput, pInput, op);
		}
		const void* Operate(const void* pInput, UnaryOpType op) const override
		{
			T& output = PreservedValue<T>::Data;
			ValueUnaryOp::Operate<T>(&output, pInput, op);
			return &output;
		}
		void Operate(IMemory* pMemory, IPin* pOutput, IPin* pInput, UnaryOpType op) const override
		{
			T output;
			ValueUnaryOp::Operate<T>(&output, pInput->GetValuePtr(pMemory), op);
			pOutput->SetValue(pMemory, &output);
		}
	};


	/////////////////////////////////////
	/////////////////////////////////////
	/////////////////////////////////////
	class DataUnaryOpMgr : public Singleton<DataUnaryOpMgr>
	{
		/// <summary>
		/// Left, Right0, Right1
		/// </summary>
		small_map<TYPEID, const IDataUnaryOpHelper*> m_UnaryOps;
	public:
		DataUnaryOpMgr();
		~DataUnaryOpMgr();

		const IDataUnaryOpHelper* Get(TYPEID t) const;

		template<typename T>
		const IDataUnaryOpHelper* Get() const
		{
			return Get(GetTypeID<T>());
		}
	};
}
