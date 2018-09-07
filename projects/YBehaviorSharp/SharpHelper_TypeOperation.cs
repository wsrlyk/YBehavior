using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    using INT = System.Int32;
    using BOOL = System.Int16;
    using FLOAT = System.Single;
    using ULONG = System.UInt64;
    using STRING = System.String;
    using Bool = System.Int16;
    using KEY = System.Int32;

    public partial class SharpHelper
    {
        ///////////////////////////////////////////////////////////////
        
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetVariableValue(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableValue(IntPtr pAgent, IntPtr pVariable, IntPtr pValue);

        [DllImport(VERSION.dll)]
        static public extern int GetVariableInt(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableInt(IntPtr pAgent, IntPtr pVariable, int value);

        [DllImport(VERSION.dll)]
        static public extern ulong GetVariableUlong(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableUlong(IntPtr pAgent, IntPtr pVariable, ulong value);

        [DllImport(VERSION.dll)]
        static public extern float GetVariableFloat(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableFloat(IntPtr pAgent, IntPtr pVariable, float value);

        [DllImport(VERSION.dll)]
        static public extern BOOL GetVariableBool(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableBool(IntPtr pAgent, IntPtr pVariable, BOOL value);

        [DllImport(VERSION.dll)]
        static public extern string GetVariableString(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableString(IntPtr pAgent, IntPtr pVariable, string value);

        [DllImport(VERSION.dll)]
        static public extern Vector3 GetVariableVector3(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableVector3(IntPtr pAgent, IntPtr pVariable, Vector3 value);

        [DllImport(VERSION.dll)]
        static public extern IntPtr GetEntityFromWrapper(IntPtr pWrapper);
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetEntityFromVariable(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetEntityToVariable(IntPtr pAgent, IntPtr pVariable, IntPtr value);

        ///////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static public extern int GetSharedDataInt(IntPtr pAgent, int key);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedDataInt(IntPtr pAgent, int key, int value);

        [DllImport(VERSION.dll)]
        static public extern ulong GetSharedDataUlong(IntPtr pAgent, int key);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedDataUlong(IntPtr pAgent, int key, ulong value);

        [DllImport(VERSION.dll)]
        static public extern float GetSharedDataFloat(IntPtr pAgent, int key);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedDataFloat(IntPtr pAgent, int key, float value);

        [DllImport(VERSION.dll)]
        static public extern BOOL GetSharedDataBool(IntPtr pAgent, int key);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedDataBool(IntPtr pAgent, int key, BOOL value);

        [DllImport(VERSION.dll)]
        static public extern Vector3 GetSharedDataVector3(IntPtr pAgent, int key);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedDataVector3(IntPtr pAgent, int key, Vector3 value);

        [DllImport(VERSION.dll)]
        static public extern string GetSharedDataString(IntPtr pAgent, int key);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedDataString(IntPtr pAgent, int key, string value);

        ///////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static public extern int GetIntVectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearIntVector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackIntVector(IntPtr pVector, int value);
        [DllImport(VERSION.dll)]
        static public extern void SetIntVectorAtIndex(IntPtr pVector, int index, int value);
        [DllImport(VERSION.dll)]
        static public extern int GetIntVectorAtIndex(IntPtr pVector, int index);

        [DllImport(VERSION.dll)]
        static public extern int GetUlongVectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearUlongVector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackUlongVector(IntPtr pVector, ulong value);
        [DllImport(VERSION.dll)]
        static public extern void SetUlongVectorAtIndex(IntPtr pVector, int index, ulong value);
        [DllImport(VERSION.dll)]
        static public extern ulong GetUlongVectorAtIndex(IntPtr pVector, int index);

        [DllImport(VERSION.dll)]
        static public extern int GetFloatVectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearFloatVector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackFloatVector(IntPtr pVector, float value);
        [DllImport(VERSION.dll)]
        static public extern void SetFloatVectorAtIndex(IntPtr pVector, int index, float value);
        [DllImport(VERSION.dll)]
        static public extern float GetFloatVectorAtIndex(IntPtr pVector, int index);

        [DllImport(VERSION.dll)]
        static public extern int GetBoolVectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearBoolVector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackBoolVector(IntPtr pVector, BOOL value);
        [DllImport(VERSION.dll)]
        static public extern void SetBoolVectorAtIndex(IntPtr pVector, int index, BOOL value);
        [DllImport(VERSION.dll)]
        static public extern BOOL GetBoolVectorAtIndex(IntPtr pVector, int index);

        [DllImport(VERSION.dll)]
        static public extern int GetVector3VectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearVector3Vector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackVector3Vector(IntPtr pVector, Vector3 value);
        [DllImport(VERSION.dll)]
        static public extern void SetVector3VectorAtIndex(IntPtr pVector, int index, Vector3 value);
        [DllImport(VERSION.dll)]
        static public extern Vector3 GetVector3VectorAtIndex(IntPtr pVector, int index);

        [DllImport(VERSION.dll)]
        static public extern int GetStringVectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearStringVector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackStringVector(IntPtr pVector, string value);
        [DllImport(VERSION.dll)]
        static public extern void SetStringVectorAtIndex(IntPtr pVector, int index, string value);
        [DllImport(VERSION.dll)]
        static public extern string GetStringVectorAtIndex(IntPtr pVector, int index);

        [DllImport(VERSION.dll)]
        static public extern int GetEntityVectorSize(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void ClearEntityVector(IntPtr pVector);
        [DllImport(VERSION.dll)]
        static public extern void PushBackEntityVector(IntPtr pVector, IntPtr pEntity);
        [DllImport(VERSION.dll)]
        static public extern void SetEntityVectorAtIndex(IntPtr pVector, int index, IntPtr pEntity);
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetEntityVectorAtIndex(IntPtr pVector, int index);

        ///////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////

        [DllImport(VERSION.dll)]
        static public extern KEY GetIntKeyByName(string name);
        [DllImport(VERSION.dll)]
        static public extern KEY GetFloatKeyByName(string name);
        [DllImport(VERSION.dll)]
        static public extern KEY GetUlongKeyByName(string name);
        [DllImport(VERSION.dll)]
        static public extern KEY GetBoolKeyByName(string name);
        [DllImport(VERSION.dll)]
        static public extern KEY GetVector3KeyByName(string name);
        [DllImport(VERSION.dll)]
        static public extern KEY GetEntityWrapperKeyByName(string name);
        [DllImport(VERSION.dll)]
        static public extern KEY GetStringKeyByName(string name);
    }
}
