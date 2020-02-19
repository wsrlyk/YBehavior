using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    [Serializable]
    public class Suo
    {
        [Serializable]
        public class SuoData
        {
            Dictionary<uint, int> m_DebugPoints = null;
            HashSet<uint> m_Fold = null;

            public int GetDebugPoint(uint uid)
            {
                if (m_DebugPoints == null)
                    return 0;
                if (m_DebugPoints.TryGetValue(uid, out var res))
                    return res;
                return 0;
            }

            public void SetDebugPoint(uint uid, int state)
            {
                if (m_DebugPoints == null)
                    m_DebugPoints = new Dictionary<uint, int>();
                if (state == 0)
                    m_DebugPoints.Remove(uid);
                else
                    m_DebugPoints[uid] = state;
            }

            public bool GetFold(uint uid)
            {
                if (m_Fold == null)
                    return false;

                return m_Fold.Contains(uid);
            }

            public void SetFold(uint uid, bool bFold)
            {
                if (m_Fold == null)
                    m_Fold = new HashSet<uint>();
                if (bFold)
                    m_Fold.Add(uid);
                else
                    m_Fold.Remove(uid);
            }
        }

        Dictionary<string, SuoData> m_Suos = new Dictionary<string, SuoData>();

        public IEnumerable<string> Files { get { return m_Suos.Keys; } }

        public SuoData GetDebugPointInfo(string fileName)
        {
            if (m_Suos.TryGetValue(fileName, out var res))
            {
                return res;
            }
            return null;
        }

        public void ResetFile(string fileName)
        {
            m_Suos.Remove(fileName);
            m_CachedFileName = string.Empty;
        }

        bool _FetchData(string fileName)
        {
            if (m_CachedFileName != fileName)
            {
                if (m_Suos.TryGetValue(fileName, out var res))
                {
                    m_CachedData = res;
                }
                else
                {
                    m_CachedData = new SuoData();
                    m_Suos[fileName] = m_CachedData;
                }
                m_CachedFileName = fileName;
            }

            if (m_CachedData == null)
            {
                LogMgr.Instance.Error("m_CachedData == null");
                return false;
            }

            return true;
        }

        public void SetDebugPointInfo(string fileName, uint uid, int debugpoint)
        {
            if (_FetchData(fileName))
            {
                m_CachedData.SetDebugPoint(uid, debugpoint);
            }
        }

        public void SetFoldInfo(string fileName, uint uid, bool bFold)
        {
            if (_FetchData(fileName))
            {
                m_CachedData.SetFold(uid, bFold);
            }
        }

        [NonSerialized]
        string m_CachedFileName;
        [NonSerialized]
        SuoData m_CachedData;
    }
}
