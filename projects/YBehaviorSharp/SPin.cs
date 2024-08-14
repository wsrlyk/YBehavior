﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using INT = System.Int32;
    using BOOL = System.Byte;

    public enum EPinCreateFlag
    {
        None = 0,
        NoConst = 1,
        IsOutput = 3,
    }
    public partial class SharpHelper
    {
        /// <summary>
        /// Create a pin object
        /// </summary>
        /// <param name="pNode">Pointer to the tree node in cpp</param>
        /// <param name="attrName">Attribute name</param>
        /// <param name="data">Pointer to the config in cpp</param>
        /// <param name="noConst">If true, the pin must be assigned with a variable</param>
        /// <returns></returns>
        public static SPin? CreatePin(IntPtr pNode, string attrName, IntPtr data, EPinCreateFlag flag = EPinCreateFlag.None)
        {
            IntPtr v = SUtility.CreatePin(pNode, attrName, data, (int)flag);
            if (v == IntPtr.Zero)
                return null;
            return SPinHelper.GetPin(v);
        }
    }
    /// <summary>
    /// Utilities of pin
    /// </summary>
    internal class SPinHelper
    {
        public static SPin GetPin(IntPtr ptr)
        {
            var type = SUtility.GetPinTypeID(ptr);
            var elementtype = SUtility.GetPinElementTypeID(ptr);
            if (type == elementtype)
            {
                var t = s_PinTypes[type];
                return Activator.CreateInstance(t, ptr) as SPin;
            }
            return new SArrayPin(ptr);
        }

        static System.Type[] s_PinTypes = new Type[7];
        static SPinHelper()
        {
            s_PinTypes[GetType<int>.ID] = typeof(SPinInt);
            s_PinTypes[GetType<float>.ID] = typeof(SPinFloat);
            s_PinTypes[GetType<ulong>.ID] = typeof(SPinUlong);
            s_PinTypes[GetType<bool>.ID] = typeof(SPinBool);
            s_PinTypes[GetType<Vector3>.ID] = typeof(SPinVector3);
            s_PinTypes[GetType<string>.ID] = typeof(SPinString);
            s_PinTypes[GetType<IEntity>.ID] = typeof(SPinEntity);
        }
    }

    /// <summary>
    /// Base class of a pin
    /// </summary>
    public class SPin
    {
        /// <summary>
        /// Pointer to the pin in cpp
        /// </summary>
        public IntPtr Ptr { get; protected set; } = IntPtr.Zero;
        /// <summary>
        /// TypeID of the pin
        /// </summary>
        public TYPEID TypeID { get; protected set; }
        /// <summary>
        /// Whether the pointer is null
        /// </summary>
        public bool IsValid { get { return Ptr != IntPtr.Zero; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptr">Pointer to the pin in cpp</param>
        public SPin(IntPtr ptr)
        {
            Ptr = ptr;
            TypeID = SUtility.GetPinTypeID(ptr);
        }
    }
    /// <summary>
    /// Array type pin
    /// </summary>
    public class SArrayPin : SPin
    {
        /// <summary>
        /// The TypeID of element of array
        /// </summary>
        public TYPEID ElementTypeID { get; protected set; }
        protected ISArray m_Array;

        public SArrayPin(IntPtr core) : base(core)
        {
            ElementTypeID = SUtility.GetPinElementTypeID(core);
            m_Array = SArrayHelper.GetArray(IntPtr.Zero, ElementTypeID);
        }

        /// <summary>
        /// Get the array object 
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <returns></returns>
        public ISArray Get(IntPtr pAgent)
        {
            IntPtr ptr = SUtility.GetPinValuePtr(pAgent, Ptr);
            m_Array.Init(ptr);
            return m_Array;
        }

        ///> No Set. Just Get and operate it.
        //public void Set(IntPtr pAgent, ISArray data)
        //{

        //}
    }

    public abstract class SPin<T> : SPin
    {
        public SPin(IntPtr core)
            : base(core)
        {
            if (core != IntPtr.Zero && GetType<T>.ID != TypeID)
            {
                Ptr = IntPtr.Zero;
            }
        }
        /// <summary>
        /// Get the data from the pin
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <returns></returns>
        abstract public T Get(IntPtr pAgent);
        /// <summary>
        /// Set the data to the pin
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="data"></param>
        abstract public void Set(IntPtr pAgent, T data);
    }

    ////////////////////////////////////////////////////////////////

    /// <summary>
    /// Entity type pin
    /// </summary>
    public class SPinEntity : SPin
    {
        public SPinEntity(IntPtr core) : base(core)
        {
            if (core != IntPtr.Zero && GetType<IEntity>.ID != TypeID)
            {
                Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Get the data from the pin
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <returns></returns>
        public IEntity? Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SPtrMgr.Instance.Get(SUtility.GetFromBufferEntity()) as IEntity;
            }
            return null;
        }

        /// <summary>
        /// Set the data to the pin
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="data"></param>
        public void Set(IntPtr pAgent, IEntity? data)
        {
            SUtility.SetToBufferEntity(data == null ? IntPtr.Zero : data.Ptr);
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }

    /// <summary>
    /// Int type pin
    /// </summary>
    public class SPinInt : SPin<int>
    {
        public SPinInt(IntPtr core) : base(core) { }

        public override int Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SUtility.GetFromBufferInt();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, int data)
        {
            SUtility.SetToBufferInt(data);
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }

    /// <summary>
    /// Float type pin
    /// </summary>
    public class SPinFloat : SPin<float>
    {
        public SPinFloat(IntPtr core) : base(core) { }

        public override float Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SUtility.GetFromBufferFloat();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, float data)
        {
            SUtility.SetToBufferFloat(data);
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }

    /// <summary>
    /// Ulong type pin
    /// </summary>
    public class SPinUlong : SPin<ulong>
    {
        public SPinUlong(IntPtr core) : base(core) { }

        public override ulong Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SUtility.GetFromBufferUlong();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, ulong data)
        {
            SUtility.SetToBufferUlong(data);
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }

    /// <summary>
    /// Bool type pin
    /// </summary>
    public class SPinBool : SPin<bool>
    {
        public SPinBool(IntPtr core) : base(core) { }

        public override bool Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.ConvertBool(SUtility.GetFromBufferBool());
            }
            return false;
        }

        public override void Set(IntPtr pAgent, bool data)
        {
            SUtility.SetToBufferBool(SharpHelper.ConvertBool(data));
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }

    /// <summary>
    /// Vector3 type pin
    /// </summary>
    public class SPinVector3 : SPin<Vector3>
    {
        public SPinVector3(IntPtr core) : base(core) { }

        public override Vector3 Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SUtility.GetFromBufferVector3();
            }
            return Vector3.zero;
        }

        public override void Set(IntPtr pAgent, Vector3 data)
        {
            SUtility.SetToBufferVector3(data);
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }

    /// <summary>
    /// String type pin
    /// </summary>
    public class SPinString : SPin<string>
    {
        public SPinString(IntPtr core) : base(core) { }

        public override string Get(IntPtr pAgent)
        {
            if (SUtility.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferString();
            }
            return string.Empty;
        }

        public override void Set(IntPtr pAgent, string data)
        {
            SharpHelper.SetToBufferString(data);
            SUtility.SetPinValue(pAgent, Ptr);
        }
    }
}
