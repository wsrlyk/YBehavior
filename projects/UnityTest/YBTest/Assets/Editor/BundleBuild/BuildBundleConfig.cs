using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public enum BuildPlatform
{
    IOS = 0x0001,
    Android = 0x0002,
    PC = 0x0004,
}

public enum BundleType
{
    INVALID_ASSET,
    SINGLE_BUNDLE,
    REFERENCE_ASSET,
    PACKAGE_BUNDLE,
    SCENE_REDIRECT_ASSET,
    CONFIG_REDIRECT_ASSET,
    SCENE_ASSET,
    EXPORT_ASSET,
    PREFAB_REDIRECT_ASSET,
    PREFAB_ASSET,
    NUM
}

public class BuildBundleContext
{
    public HashSet<string> pathKey = new HashSet<string> ();
    public HashSet<string> physicPathKey = new HashSet<string> ();
    public Dictionary<string, List<string>> packageBundle = new Dictionary<string, List<string>> ();
    public BundleLogger bundleLogger = new BundleLogger ();
    public int count = 0;
    public void Clear ()
    {
        pathKey.Clear ();
        physicPathKey.Clear ();
        packageBundle.Clear ();
        bundleLogger.Clear ();
        count = 0;
    }
}

[Serializable]
public class BundlePath
{
    public string dir;
    public string assetBundleName;
    [NonSerialized]
    public List<string> assetNames;
    public bool enable = true;
    [NonSerialized]
    public List<AssetBundleBuild> bundleList = new List<AssetBundleBuild> ();
    [NonSerialized]
    public bool bundleFolder = false;
    public void PreBuild (BuildBundleContext context)
    {
        if (assetNames != null)
        {
            assetNames.Clear ();
        }
        bundleList.Clear ();
    }

    public void PostBuild (BuildBundleContext context)
    {
        if (!string.IsNullOrEmpty (assetBundleName) &&
            assetNames != null && assetNames.Count > 0)
        {
            List<string> names;
            if (!context.packageBundle.TryGetValue (assetBundleName, out names))
            {
                names = new List<string> ();
                context.packageBundle.Add (assetBundleName, names);
            }
            names.AddRange (assetNames);
            bundleList.Add (new AssetBundleBuild ()
            {
                assetBundleName = assetBundleName,
                    assetNames = assetNames.ToArray ()
            });
        }
    }

    public void AddBundle (BuildBundleContext context, string name, string path)
    {
        if (!context.pathKey.Contains (name))
        {
            string physicPathLow = path.ToLower();
            if (!context.physicPathKey.Contains (physicPathLow))
            {
                AssetBundleBuild build = new AssetBundleBuild ();

                build.assetBundleName = name;
                build.assetNames = new string[] { path };
                bundleList.Add (build);
                context.count++;
                context.bundleLogger.AddBundleNameList (name, path);
                context.pathKey.Add (name);
                context.physicPathKey.Add (physicPathLow);
            }
            else
            {
                Debug.LogError(string.Format("duplicate path:{0}\r\nres:{1}", name, path));
            }

        }
    }
}

public class ReportItem
{
    public string name = "";
    public long sizeInByte = 0;
}
public class ReportGroup
{
    public int groupType = 0;
    public long sizeInByte = 0;
    public List<ReportItem> items = new List<ReportItem> ();
}
public class BuildBundleConfig //: AssetBaseConifg<BuildBundleConfig>
{
    public static BuildBundleConfig Instance { get; } = new BuildBundleConfig();
        
    public List<BundlePath> configs = new List<BundlePath> ();
    public BuildBundleContext context = new BuildBundleContext ();
    //public List<PreBuildPreProcess> buildProcess = new List<PreBuildPreProcess> ();
    public Dictionary<string, ReportGroup> resReport = new Dictionary<string, ReportGroup> ();
    public long timeHash = 0;

    public static bool build = false;

    public BuildBundleConfig()
    {
        BundlePath path;

        path = new BundlePath();
        path.dir = "Assets/Res";
        path.enable = true;
        configs.Add(path);

        path = new BundlePath();
        path.dir = "Assets/Scenes";
        path.enable = true;
        configs.Add(path);
    }
    ///> 打ab之前执行
    public void OnPrebuildBundle (bool build)
    {
        //PreBuildPreProcess.build = build;
        //PreBuildPreProcess.count = 0;
        //buildProcess.Clear ();
        //var types = EditorCommon.GetAssemblyType (typeof (PreBuildPreProcess));
        //foreach (var t in types)
        //{
        //    var process = Activator.CreateInstance (t) as PreBuildPreProcess;
        //    if (process != null)
        //    {
        //        buildProcess.Add (process);
        //    }
        //}
        //buildProcess.Sort ((x, y) => x.Priority - y.Priority);
        //for (int i = 0; i < buildProcess.Count; ++i)
        //{
        //    var bp = buildProcess[i];
        //    bp.PreProcess ();
        //}
        //if (build)
        //{
        //    timeHash = DateTime.Now.ToFileTime ();
        //    string versionPath = string.Format ("{0}/Version/PackageBytesVersion.bytes",
        //        AssetsConfig.instance.ResourcePath);
        //    if (File.Exists (versionPath))
        //        AssetDatabase.DeleteAsset (versionPath);
        //    using (FileStream fs = new FileStream (versionPath, FileMode.Create))
        //    {
        //        BinaryWriter bw = new BinaryWriter (fs);
        //        bw.Write (timeHash);
        //    }
        //    AssetDatabase.ImportAsset (versionPath, ImportAssetOptions.ForceUpdate);
        //}

    }

    private void BuildBundle (BundlePath bp, DirectoryInfo di)
    {
        DirectoryInfo[] subDis = di.GetDirectories ("*.*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < subDis.Length; ++i)
        {
            var subDi = subDis[i];
            string name = subDi.Name.ToLower ();
            if (!name.Contains ("editor") &&
                !name.Contains ("test") &&
                !name.Contains ("testignore"))
            {
                BuildBundle (bp, subDi);
            }
        }

        FileInfo[] fileInfos = di.GetFiles ("*.*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < fileInfos.Length; ++i)
        {
            var fi = fileInfos[i];
            if (fi.Extension != ".meta")
            {
                string name = fi.Name;
                //if (!name.Contains ("test") &&
                //    !name.Contains ("testignore"))
                {

                    string fullName = fi.FullName.ToLower();
                    fullName = fullName.Replace ('\\', '/');
                    int index = fullName.IndexOf ("assets/");
                    fullName = fullName.Substring (index);

                    //                        for (int j = 0; j < bp.rules.Count; ++j)
                    //                        {
                    //                            var rule = bp.rules[j];
                    //                            if (fullName.EndsWith (rule.path.ToLower ()) && !context.pathKey.Contains (fullName))
                    //                            {
                    //                                var ruleCb = BundleRule.ruleCb[(int) rule.op];
                    //                                if (ruleCb != null)
                    //                                {
                    //                                    bool build = false;
                    //#if UNITY_IOS
                    //                                    build = (rule.buildPlatform & (uint) BuildPlatform.IOS) != 0;
                    //#else

                    //#if UNITY_ANDROID
                    //                                    build = (rule.buildPlatform & (uint) BuildPlatform.Android) != 0;
                    //#else
                    //                                    build = (rule.buildPlatform & (uint) BuildPlatform.PC) != 0;
                    //#endif
                    //#endif

                    //                                    if (build)
                    {
                        string n = fullName;
                        if (n.EndsWith(".unity"))
                        {
                            n = n.Substring(0, n.IndexOf('.'));
                        }

                                    bp.AddBundle(context, n, fullName);

                        //ruleCb(context, fullName, bp);
                    }
                                context.pathKey.Add (fullName);
                    //        }
                    //    }
                    //}
                }
            }

        }

    }
    public void BuildBundle (bool build, bool quiet = false, int index = -1, bool buildAll = true, bool removeManifest = true)
    {
        BuildBundleConfig.build = build;
        context.Clear ();
        context.bundleLogger.quiet = quiet;
        bool needCopyManifest = false;
        string spath = "Assets/StreamingAssets/Bundles";
        if (build)
        {

            if (!AssetDatabase.IsValidFolder (spath))
            {
                AssetDatabase.CreateFolder ("Assets/StreamingAssets", "Bundles");
            }
            //PrefabConfigTool.RefreshPrefabFolder (EditorPrefabData.instance, new PrefabConvertContext ());
            //PrefabConfigTool.SavePrefabData (EditorPrefabData.instance);
            OnPrebuildBundle (index == -1 ? build : false);
        }

        if (index >= 0 && index < configs.Count)
        {
            var bp = configs[index];
            if (bp.enable)
            {
                DirectoryInfo di = new DirectoryInfo (bp.dir);
                bp.PreBuild (context);
                BuildBundle (bp, di);
                bp.PostBuild (context);
            }

        }
        else
        {
            for (int i = 0; i < configs.Count; ++i)
            {
                var bp = configs[i];
                if (bp.enable || buildAll)
                {
                    DirectoryInfo di = new DirectoryInfo (bp.dir);
                    bp.PreBuild (context);
                    BuildBundle (bp, di);
                    bp.PostBuild (context);
                }
                needCopyManifest |= !bp.enable;
            }
        }
        List<AssetBundleBuild> bundleList = new List<AssetBundleBuild> ();
        foreach (var kvp in context.packageBundle)
        {
            if (!context.pathKey.Contains (kvp.Key))
            {
                AssetBundleBuild abb = new AssetBundleBuild ();
                abb.assetBundleName = kvp.Key;
                abb.assetNames = kvp.Value.ToArray ();
                bundleList.Add (abb);
                context.count++;
                context.bundleLogger.AddBundleNameList (kvp.Key, kvp.Key);
                context.pathKey.Add (kvp.Key);
            }
        }

        if (build)
        {
            // OnPrebuildBundle (index == -1 ? build : false);

            for (int i = 0; i < configs.Count; ++i)
            {
                var bp = configs[i];
                if (string.IsNullOrEmpty (bp.assetBundleName))
                    bundleList.AddRange (bp.bundleList);
            }

            bool copyManifest = false;
            string manifest = "Assets/StreamingAssets/Bundles/Bundles";
            string manifestBack = "Assets/Bundles";
            if (index >= 0 && index < configs.Count || needCopyManifest)
            {
                if (File.Exists (manifest))
                {
                    File.Copy (manifest, manifestBack, true);
                    copyManifest = true;
                }
            }
            BuildPipeline.BuildAssetBundles (spath, bundleList.ToArray (),
                BuildAssetBundleOptions.ChunkBasedCompression |
                BuildAssetBundleOptions.DeterministicAssetBundle,
                EditorUserBuildSettings.activeBuildTarget);
            EditorUtility.ClearProgressBar ();
            AssetDatabase.RemoveUnusedAssetBundleNames ();
            if (copyManifest && File.Exists (manifestBack))
            {
                File.Copy (manifestBack, manifest, true);
            }
            AssetDatabase.SaveAssets ();
            AssetDatabase.Refresh ();
            if (removeManifest)
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo (spath);
                    FileInfo[] files = di.GetFiles ("*.manifest", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        if (files[i].Name != "Bundles.manifest")
                            files[i].Delete ();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
            EditorUtility.DisplayDialog ("build bundle finish", " build finish", "OK");
            // context.bundleLogger.BuildLog ();
            BuildReport ("Assets/StreamingAssets/Bundles/assets/res/");
        }
    }

    private void BuildReport (string path)
    {
        resReport.Clear ();
        long totalSize = 0;
        DirectoryInfo di = new DirectoryInfo (path);
        var subDirs = di.GetDirectories ("*.*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < subDirs.Length; ++i)
        {
            var subDir = subDirs[i];
            var group = new ReportGroup () { groupType = 0 };
            resReport.Add (subDir.Name, group);

            var files = subDir.GetFiles ("*.*", SearchOption.AllDirectories);
            for (int j = 0; j < files.Length; ++j)
            {
                var file = files[j];
                string ext = file.Extension.ToLower ();
                if (ext != ".manifest" && ext != ".meta")
                {
                    ReportGroup typeGroup;
                    if (!resReport.TryGetValue (ext, out typeGroup))
                    {
                        typeGroup = new ReportGroup () { groupType = 1 };
                        resReport.Add (ext, typeGroup);
                    }
                    string filename = file.FullName.Replace ("\\", "/");
                    int index = filename.IndexOf (path);
                    if (index > 0)
                    {
                        filename = filename.Substring (index + path.Length);
                    }

                    var ri = new ReportItem ()
                    {
                        name = filename,
                        sizeInByte = file.Length
                    };
                    typeGroup.items.Add (ri);
                    typeGroup.sizeInByte += ri.sizeInByte;
                    group.items.Add (ri);
                    group.sizeInByte += ri.sizeInByte;
                    totalSize += ri.sizeInByte;
                }
            }
        }

        var bankgroup = new ReportGroup () { groupType = 1 };
        resReport.Add (".bank", bankgroup);
        var parentDi = di.Parent;
        var bankfiles = parentDi.GetFiles ("*.bank", SearchOption.TopDirectoryOnly);

        for (int j = 0; j < bankfiles.Length; ++j)
        {
            var file = bankfiles[j];
            bankgroup.sizeInByte += file.Length;
            totalSize += bankgroup.sizeInByte;
            var ri = new ReportItem ()
            {
                name = file.Name,
                sizeInByte = file.Length
            };
            bankgroup.items.Add (ri);
        }

        try
        {
            using (FileStream fs = new FileStream ("Assets/BuildLog.txt", FileMode.Create))
            {
                var sb = new System.Text.StringBuilder ();
                sb.AppendLine ("==========================build bundle report==========================");
                sb.AppendLine ("time hash:" + timeHash.ToString ());
                sb.AppendLine ("\t\t\tTotal Size: " + EditorUtility.FormatBytes (totalSize));
                var sw = new StreamWriter (fs);
                sb.AppendLine ("==========================folder group==========================");
                BuildReport (0, false, sb, sw);
                sb.AppendLine ();
                sb.AppendLine ("==========================res group==========================");
                BuildReport (1, true, sb, sw);
                sw.Write (sb.ToString ());
            }

        }
        catch (Exception e)
        {
            Debug.LogError("save build log error:" + e.Message);
        }

    }

    private void BuildReport (int groupType, bool outputFileInfo, System.Text.StringBuilder sb, StreamWriter sw)
    {
        var it = resReport.GetEnumerator ();
        while (it.MoveNext ())
        {
            var current = it.Current;
            var v = current.Value;
            if (v.groupType == groupType)
            {
                sb.Append ("Group:");
                sb.Append (current.Key);
                int spaceCount = 40 - current.Key.Length;
                for (int i = 0; i < spaceCount; ++i)
                {
                    sb.Append (" ");
                }
                sb.Append (string.Format ("Count: {0} Size: {1} {2} Bytes\r\n",
                    v.items.Count.ToString (),
                    EditorUtility.FormatBytes (v.sizeInByte),
                    v.sizeInByte.ToString ()));
                if (outputFileInfo)
                {
                    sb.AppendLine ();
                    v.items.Sort ((x, y) => x.sizeInByte.CompareTo (y.sizeInByte));
                    for (int i = v.items.Count - 1; i >= 0; --i)
                    {
                        var item = v.items[i];
                        sb.Append ("\t");
                        sb.Append (item.name);
                        spaceCount = 80 - item.name.Length;
                        for (int j = 0; j < spaceCount; ++j)
                        {
                            sb.Append (" ");
                        }
                        sb.Append (string.Format ("Size: {0} {1} Bytes\r\n",
                            EditorUtility.FormatBytes (item.sizeInByte),
                            item.sizeInByte.ToString ()));
                    }
                }
            }
        }
        // DebugLog.AddLog (sb.ToString ());
    }
}
