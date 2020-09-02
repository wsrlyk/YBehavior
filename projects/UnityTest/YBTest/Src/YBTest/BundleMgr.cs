using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace YBTest
{
    public enum BUNDLE_STATE
    {
        LOADED,
        IN_LOAD_QUEUE,
        TOBE_UNLOAD_TRUE,
        TOBE_UNLOAD_FALSE,
        UNLOADED,
    }

    public class BundleData
    {

        public AssetBundle bundle;

        public int reference;

        public BUNDLE_STATE state;

        public int unload_record_time;

        public void Clear()
        {
            bundle = null;
        }
    }


    public class BundleMgr : Singleton<BundleMgr>
    {
        private AssetBundleManifest manifest = null;

        private Hash128 zeroHash = new Hash128(0, 0, 0, 0);

        // bundle name -> client bundle
        private Dictionary<string, BundleData> _bundles = new Dictionary<string, BundleData>();

        private bool bManifestLoaded = false;
        public static string BundleFolder = "Bundles";
        public static string BundleRootDefault = null; //Application.dataPath + "/StreamingAssets" + BundleFolder;
        public static string PersistRootDefault = null;

        // bundles referenced by other bundle 
        private HashSet<string> _bundle_ref = new HashSet<string>();

        public HashSet<string> _patchFiles { get; set; }

        public void Init()
        {
            SetupPath();
            LoadManifest();
        }

        private static bool IsNoDependencyAsset(Type t)
        {
            return t == typeof(Texture) || t == typeof(AnimationClip) || t == typeof(TextAsset) || t == typeof(Mesh);
        }

        private void SetupPath()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    BundleRootDefault = string.Format("{0}!assets/{1}/", Application.dataPath, BundleFolder);
                    break;
                case RuntimePlatform.IPhonePlayer:
                    BundleRootDefault = string.Format("{0}/Raw/{1}/", Application.dataPath, BundleFolder);
                    break;
                default:
                    BundleRootDefault = string.Format("{0}/StreamingAssets/{1}/", Application.dataPath, BundleFolder);
                    break;
            }
        }

        public static void LoadErrorLog(string prefab)
        {
            Debug.LogError("Load Error " + prefab);
        }


        public void UpdateOrAddPatch(string location)
        {
            if (_patchFiles == null)
            {
                _patchFiles = new HashSet<string>();
                PersistRootDefault = Path.Combine(Application.persistentDataPath, "update");
            }
            _patchFiles.Add(location);
        }

        private AssetBundle LoadFromFile(string bundleName)
        {
            if (ContainsPatch(bundleName, out var name))
            {
                var path = Path.Combine(PersistRootDefault, name);
                return AssetBundle.LoadFromFile(path);
            }
            else
            {
                return AssetBundle.LoadFromFile(BundleRootDefault + bundleName);
            }
        }

        private string ManifestPath
        {
            get
            {
                var folder = BundleFolder.ToLower();
                if (ContainsPatch(folder, out var name))
                {
                    return Path.Combine(PersistRootDefault, folder);
                }
                else
                {
                    return BundleRootDefault + BundleFolder;
                }
            }
        }

        public bool ContainsPatch(string bundleName, out string name)
        {
            if (_patchFiles != null)
            {
                name = bundleName.Replace("assets/bundleres/", string.Empty);
                return _patchFiles.Contains(name);
            }
            name = bundleName;
            return false;
        }

        private BundleData LoadBundle(string bundleName, bool showError = true)
        {
            BundleData cb = null;

            Hash128 hash = manifest.GetAssetBundleHash(bundleName);

            if (_bundles.ContainsKey(bundleName))
            {
                if (_bundles[bundleName].reference <= 0 && _bundles[bundleName].state == BUNDLE_STATE.UNLOADED)
                {
                    if (hash == zeroHash)
                    {
                        if (showError) Debug.LogError("Bundle not exists:" + bundleName);
                        return null;
                    }
                    AssetBundle ab = LoadFromFile(bundleName);
                    _bundles[bundleName].bundle = ab;
                }

                _bundles[bundleName].reference = _bundles[bundleName].reference <= 0 ? 1 : _bundles[bundleName].reference + 1;
                _bundles[bundleName].state = BUNDLE_STATE.LOADED;

                return _bundles[bundleName];
            }
            else
            {
                if (hash == zeroHash)
                {
                    if (showError) LoadErrorLog(bundleName);
                    //DebugLog.AddErrorLog("Bundle not exists:" + bundleName);
                    return null;
                }

                AssetBundle ab = LoadFromFile(bundleName);
                if (ab == null) return null;

                cb = new BundleData();
                cb.bundle = ab;
                cb.reference = 1;
                cb.state = BUNDLE_STATE.LOADED;
                _bundles.Add(bundleName, cb);
            }

            return cb;
        }

        private bool IsBundleLoaded(string bundleName)
        {
            BundleData cb;

            if (_bundles.TryGetValue(bundleName, out cb))
            {
                if (cb.reference > 0 && cb.state == BUNDLE_STATE.LOADED) return true;
            }

            return false;
        }

        private void UnloadBundle(string bundleName)
        {
            if (IsBundleLoaded(bundleName))
            {
                _bundles[bundleName].reference--;
                if (_bundles[bundleName].reference <= 0)
                    _bundles[bundleName].state = BUNDLE_STATE.TOBE_UNLOAD_TRUE;
            }
        }

        private void UnloadBundleImmdiately(string bundleName)
        {
            if (IsBundleLoaded(bundleName))
            {
                _bundles[bundleName].reference = 0;
                _bundles[bundleName].unload_record_time = Time.frameCount;
                _bundles[bundleName].state = BUNDLE_STATE.TOBE_UNLOAD_FALSE;
            }
        }

        public UnityEngine.Object LoadAssetFromBundle(string bundleName, bool Instance, Type t, bool showError = true)
        {
            // find reference first
            if (!IsNoDependencyAsset(t))
            {
                string[] depends = manifest.GetAllDependencies(bundleName);
                for (int i = 0; i < depends.Length; i++)
                {
                    if (!string.IsNullOrEmpty(depends[i]))
                        LoadBundle(depends[i], showError);
                }
            }


            // load myself 
            BundleData cb = LoadBundle(bundleName, showError);

            if (cb == null || cb.bundle == null)
            {
                return null;
            }

            UnityEngine.Object o = null;
            {
                string[] assetNames = cb.bundle.GetAllAssetNames();
                if (assetNames.Length > 0)
                {
                    o = cb.bundle.LoadAsset(assetNames[0]);
                    if (o == null)
                    {
                        if (showError) Debug.LogError("Load asset failed:" + bundleName + "->" + cb.bundle.GetAllAssetNames()[0]);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            if (!HasReferenced(bundleName))
            {
                UnloadBundleImmdiately(bundleName);
            }

            if (Instance)
                return UnityEngine.Object.Instantiate(o);

            return o;
        }
        public UnityEngine.Object[] LoadAssetFromBundle(string bundleName, bool showError = true)
        {
            // load myself 
            BundleData cb = LoadBundle(bundleName, showError);

            if (cb == null || cb.bundle == null)
            {
                return null;
            }

            UnityEngine.Object[] objs = cb.bundle.LoadAllAssets();

            if (!HasReferenced(bundleName))
            {
                UnloadBundleImmdiately(bundleName);
            }

            return objs;
        }

        public void UnloadBundleAsset(string bundleName, Type t = null)
        {
            if (!IsNoDependencyAsset(t))
            {
                string[] depends = manifest.GetAllDependencies(bundleName);
                for (int i = 0; i < depends.Length; i++)
                {
                    UnloadBundle(depends[i]);
                }
            }


            UnloadBundle(bundleName);
        }


        private void LoadManifest()
        {

            if (manifest != null) return;
            if (bManifestLoaded) return;
            // manifest name is always bundle folder name

            var path = ManifestPath;
            AssetBundle manifestBundle = AssetBundle.LoadFromFile(path);
            bManifestLoaded = true;
            if (manifestBundle == null)
            {
                Debug.LogError("Load Manifest Bundle Failed:" + path);
            }

            if (manifestBundle != null)
            {
                //Debug.Log("Load Manifest Bundle Success");
                manifest = (AssetBundleManifest)manifestBundle.LoadAsset("AssetBundleManifest");

                if (manifest == null)
                {
                    Debug.LogError("Load Manifest Asset Error");
                    return;
                }

                //Debug.Log("Load Manifest Asset Success");
                string[] all_bundles = manifest.GetAllAssetBundles();

                for (int i = 0; i < all_bundles.Length; i++)
                {
                    string[] depends = manifest.GetAllDependencies(all_bundles[i]);

                    for (int j = 0; j < depends.Length; j++)
                    {
                        _bundle_ref.Add(depends[j]);
                    }
                }
            }
        }

        private bool HasReferenced(string bundleName)
        {
            return _bundle_ref.Contains(bundleName);
        }

        public void DebugBundleLoadingInfo()
        {
            Debug.Log("==================START DEBUG BUNDLE INFO======================");
            foreach (KeyValuePair<string, BundleData> pair in _bundles)
            {
                Debug.Log(pair.Key + ":" + pair.Value.reference);
            }
        }

        public void UnloadUnusedBundle()
        {
            KeyValuePair<string, BundleData> pair;
            var it = _bundles.GetEnumerator();
            while (it.MoveNext())
            {
                pair = it.Current;

                if (pair.Value.reference <= 0 && pair.Value.bundle != null)
                {
                    if (pair.Value.state == BUNDLE_STATE.TOBE_UNLOAD_TRUE)
                    {
                        pair.Value.bundle.Unload(true);
                        pair.Value.bundle = null;
                        pair.Value.reference = 0;
                        pair.Value.state = BUNDLE_STATE.UNLOADED;
                    }

                    if (pair.Value.state == BUNDLE_STATE.TOBE_UNLOAD_FALSE)
                    {
                        if (Time.frameCount - pair.Value.unload_record_time > 10)
                        {
                            pair.Value.bundle.Unload(false);
                            pair.Value.bundle = null;
                            pair.Value.reference = 0;
                            pair.Value.state = BUNDLE_STATE.UNLOADED;
                        }
                    }
                }
            }
        }

    }
}
