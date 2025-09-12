#pragma once

#include "YBehavior/variable.h"
#include "YBehavior/interface.h"
#include "YBehavior/treekeymgr.h"
#include "YBehavior/memory.h"
#include <algorithm>
#include "YBehavior/logger.h"

namespace YBehavior
{
	template<typename T>
	class PinAny : public IPin
	{
	public:
		typedef T ElementType;
		typedef StdVector<T> ArrayType;
		typedef T ScalarType;
		PinAny(IPin* pPin, bool isOwner = false)
			: m_RealPin(pPin)
			, m_IsOwner(isOwner)
		{
			if (pPin->IsThisArray())
				m_ArrayPin = static_cast<Pin<StdVector<T>>*>(pPin);
			else
				m_ScalarPin = static_cast<Pin<T>*>(pPin);
		}
		~PinAny()
		{
			if (m_IsOwner)
				delete m_RealPin;
		}
	protected:
		IPin* m_RealPin{};
		bool m_IsOwner{};
		Pin<ScalarType>* m_ScalarPin{};
		Pin<ArrayType>* m_ArrayPin{};
	public:
		const ScalarType* GetScalarValue(IMemory* pMemory)
		{
			if (m_ScalarPin)
				return m_ScalarPin->GetValue(pMemory);
			return nullptr;
		}

		bool GetScalarValue(IMemory* pMemory, ScalarType& v)
		{
			if (m_ScalarPin)
			{
				m_ScalarPin->GetValue(pMemory, v);
				return true;
			}
			return false;
		}
		ArrayType* GetArrayValue(IMemory* pMemory)
		{
			if (m_ArrayPin)
				return m_ArrayPin->GetValue(pMemory);
			return nullptr;
		}

		bool GetArrayValue(IMemory* pMemory, ArrayType& v)
		{
			if (m_ArrayPin)
			{
				m_ArrayPin->GetValue(pMemory, v);
				return true;
			}
			return false;
		}

		bool SetScalarValue(IMemory* pMemory, const ScalarType& v)
		{
			if (m_ScalarPin)
			{
				m_ScalarPin->SetValue(pMemory, v);
				return true;
			}
			return false;
		}

		bool SetArrayValue(IMemory* pMemory, const ArrayType& v)
		{
			if (m_ArrayPin)
			{
				m_ArrayPin->SetValue(pMemory, v);
				return true;
			}
			return false;
		}

	//overrides:
	public:
		TYPEID TypeID() const override{ return m_RealPin->TypeID(); }
		TYPEID ElementTypeID() const override { return m_RealPin->ElementTypeID(); }
		TYPEID GetReferenceSharedDataSelfID() override{ return m_RealPin->GetReferenceSharedDataSelfID(); }
		bool IsThisArray() const override{ return m_ArrayPin != nullptr; }

		IPin* GetArrayIndex() override { return m_RealPin->GetArrayIndex(); }

		bool IsConst() const override { return m_RealPin->IsConst(); }

		INT ArraySize(IMemory* pMemory) override { return m_RealPin->ArraySize(pMemory); }

		void Clear(IMemory* pMemory) override { return m_RealPin->Clear(pMemory); }

		STRING GetValueToSTRING(IMemory* pMemory) override { return m_RealPin->GetValueToSTRING(pMemory); }
		const void* GetElementPtr(IMemory* pMemory, INT index) override { return m_RealPin->GetElementPtr(pMemory, index); }
		void SetElement(IMemory* pMemory, const void* v, INT index) override { return m_RealPin->SetElement(pMemory, v, index); }

		void PushBackElement(IMemory* pMemory, const void* v) override { return m_RealPin->PushBackElement(pMemory, v); }

		bool RemoveElement(IMemory* pMemory, const void* v, bool isAll) override { return m_RealPin->RemoveElement(pMemory, v, isAll); }

		bool HasElement(IMemory* pMemory, const void* v, INT& firstIndex) override { return m_RealPin->HasElement(pMemory, v, firstIndex); }
		INT CountElement(IMemory* pMemory, const void* v, INT& firstIndex) override { return m_RealPin->CountElement(pMemory, v, firstIndex); }

		void* GetValuePtr(IMemory* pMemory) override { return m_RealPin->GetValuePtr(pMemory); }
		void SetValue(IMemory* pMemory, const void* src) override { return m_RealPin->SetValue(pMemory, src);}

		void SetValueFromString(const STRING& str) override { return m_RealPin->SetValueFromString(str);}
		void SetKeyFromString(const STRING& s) override { return m_RealPin->SetKeyFromString(s); }

		void SetArrayIndex(const STRING& vbType, const STRING& s) override { return m_RealPin->SetArrayIndex(vbType, s); }

	};
}
