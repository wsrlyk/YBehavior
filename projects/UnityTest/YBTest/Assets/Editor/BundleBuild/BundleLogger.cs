using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

    public class BundleLogger
    {

        private List<string> _invalidList = new List<string> ();
        private List<string> _ignoreList = new List<string> ();
        private Dictionary<string, string> _bundleList = new Dictionary<string, string> ();

        public bool quiet = true;
        public void AddInvalidFile (string filePath)
        {
            if (!quiet)
                _invalidList.Add (filePath);
        }

        public void AddIgnoreFile (string filePath)
        {
            if (!quiet)
                _ignoreList.Add (filePath);
        }

        public void AddBundleNameList (string assetName, string bundleName)
        {
            if (!quiet)
            {
                if (_bundleList.ContainsKey (assetName))
                {
                    Debug.Log ("Asset assigned bundle name multiple");
                    return;
                }

                _bundleList.Add (assetName, bundleName);
            }

        }

        public void BuildLog ()
        {
            if (!quiet)
            {
                string path = Application.dataPath + "/BuildLog.txt";

                StreamWriter sw = File.CreateText (path);

                sw.WriteLine ("Error Files:");
                for (int i = 0; i < _invalidList.Count; i++)
                {
                    sw.WriteLine (_invalidList[i]);
                }

                sw.WriteLine (" ");
                sw.WriteLine ("Ignore Files:");
                for (int i = 0; i < _ignoreList.Count; i++)
                {
                    sw.WriteLine (_ignoreList[i]);
                }

                sw.WriteLine (" ");
                sw.WriteLine ("Bundle Name List:");

                foreach (KeyValuePair<string, string> pair in _bundleList)
                {
                    sw.WriteLine (pair.Key + "----->" + pair.Value);
                }
                sw.Flush ();
                sw.Close ();
            }

        }

        public Dictionary<string, string> GetBundleList ()
        {
            Dictionary<string, string> ret = new Dictionary<string, string> ();

            string path = Application.dataPath + "/BuildLog.txt";
            StreamReader sr = File.OpenText (path);

            string line = sr.ReadLine ();

            while (line != "Bundle Name List:")
            {
                line = sr.ReadLine ();
            }

            while (true)
            {
                line = sr.ReadLine ();
                if (line == null) break;
                int index = line.IndexOf ("----->");

                if (index != -1)
                {
                    string assetname = line.Substring (0, index);
                    string bundlename = line.Substring (index + 6);

                    ret.Add (assetname, bundlename);
                }

            }

            return ret;
        }

        public void Clear ()
        {
            _invalidList.Clear ();
            _ignoreList.Clear ();
            _bundleList.Clear ();
        }
    }
