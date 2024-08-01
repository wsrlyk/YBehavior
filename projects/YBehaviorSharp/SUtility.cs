using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    public class SUtility
    {
        public static bool IsSubClassOf(Type type, Type baseType)
        {
            var b = type.BaseType;
            while (b != null)
            {
                if (b.Equals(baseType))
                {
                    return true;
                }
                b = b.BaseType;
            }
            return false;
        }

        public static char POINTER_CHAR = 'P';
        public static char CONST_CHAR = 'C';
        public static char ZERO_CHAR = '\0';

        private static int MaxStringBufferLen = 1024 * 256;

        public static string StringBuffer = new string((char)0, MaxStringBufferLen);
        public static byte[] CharBuffer = new byte[MaxStringBufferLen];
        public static unsafe string BuildStringFromCharBuffer()
        {
            fixed (char* ptr = StringBuffer)
            {
                for (int i = 0, len = CharBuffer.Length; i < len; ++i)
                {
                    ptr[i] = (char)CharBuffer[i];
                    if (ptr[i] == 0)
                    {
                        *((int*)ptr - 1) = i;
                        break;
                    }
                }
            }
            return StringBuffer;
        }
    }
}
