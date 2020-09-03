using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace YBTest
{
    public class InterfaceMgr : Singleton<InterfaceMgr>
    {
        public static uint ResourceHelperID = 1;

        private Dictionary<uint, object> _interfaces = new Dictionary<uint, object>();

        public T GetInterface<T>(uint key)
        {
            object value = null;

            _interfaces.TryGetValue(key, out value);

            return (T)value;
        }

        public T AttachInterface<T>(uint key, T value)
        {
            _interfaces[key] = value;

            return value;
        }
    }

    public interface IResourceHelp
    {
        UnityEngine.Object LoadEditorResource(string path, Type t);
    }

    public class ResourceMgr : Singleton<ResourceMgr>
    {
        private bool bUseBundleInEditor = false;
        private bool bEditorMode = false;

        public static string BundlePath = "Assets/Res/";

        public ResourceMgr()
        {
            bEditorMode = (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.LinuxEditor);

            if (bEditorMode)
            {
                bUseBundleInEditor = false;
            }
            if (!bEditorMode || bUseBundleInEditor)
                BundleMgr.Instance.Init();
        }

        public T GetSharedResource<T>(string location, bool canNotNull = true, bool preload = false) where T : UnityEngine.Object
        {
            //uint hash = Hash(location, suffix);
            {
                float time = Time.time;

                UnityEngine.Object asset = null;// GetAssetInPool(hash);

                if (asset == null)
                {
                    if (bEditorMode && !bUseBundleInEditor)
                    {
                        asset = CreateFromEditorAssets<T>(BundlePath + location, canNotNull);
                    }
                    else
                    {
                        asset = CreateFromAssetBundle<T>(BundlePath + location, canNotNull);
                    }
                }

                //AssetsRefRetain(hash, location, asset);

                //LoadAsyncTask t = GetSharedTaskInQueue(hash);
                //if(t != null)
                //{
                //    t.OnComplete(asset);
                //}

                return asset as T;
            }
        }

        private UnityEngine.Object CreateFromEditorAssets<T>(string location, bool showError = true)
        {
            UnityEngine.Object o = null;

            IResourceHelp helper = InterfaceMgr.Instance.GetInterface<IResourceHelp>(InterfaceMgr.ResourceHelperID);

            if (helper != null)
                o = helper.LoadEditorResource(location, typeof(T));

            if (o == null)
            {
                if (showError) Debug.LogError(location);
                return null;
            }

            return o;
        }

        private UnityEngine.Object CreateFromAssetBundle<T>(string location, bool showError = true)
        {
            UnityEngine.Object o;
            location = location.ToLower();

            o = BundleMgr.Instance.LoadAssetFromBundle(location, false, typeof(T), showError);

            if (o == null)
            {
                if (showError) Debug.LogError(location);
                return null;
            }

            return o;
        }

    }
}
