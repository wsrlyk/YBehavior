using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class JenkinsBuild
{
    static string _targetDir = "";
    static BuildTarget _target;
    static BuildTargetGroup _group;
    static BuildOptions _build = BuildOptions.None;
    static string _identifier = "com.yb";
    static string _product = "YBTest";
    static string _version = "0.0.0";
    static string[] _scenes = null;

    public enum TPlatform { Win32, iOS, Android }
    public enum TChanel { Outer }

    static string _macro
    {
        //get { return File.ReadAllText (Application.dataPath.Replace ("Assets", "") + "Shell/macro.txt"); }
        get { return string.Empty; }
    }

    [MenuItem ("Tools/Build/Android")]
    public static void XBuildAndroid ()
    {
        SwitchPlatForm (TPlatform.Android);
        Build ();
    }

    public static void BuildAndroid ()
    {
        SwitchPlatForm (TPlatform.Android);
        BuildAB ();
        Build ();
    }

    [MenuItem ("Tools/Build/Win32")]
    public static void XBuildWin32 ()
    {
        SwitchPlatForm (TPlatform.Win32);
        Build ();
    }

    public static void BuildWin32 ()
    {
        SwitchPlatForm (TPlatform.Win32);
        BuildAB ();
        Build ();
    }

    [MenuItem ("Tools/Build/iOS")]
    public static void XBuildIOS ()
    {
        SwitchPlatForm (TPlatform.iOS);
        Build ();
    }

    public static void BuildIOS ()
    {
        SwitchPlatForm (TPlatform.iOS);
        BuildAB ();
        Build ();
    }

    [MenuItem ("Tools/Build/Bundle")]
    public static void BuildAB ()
    {
        BuildBundleConfig.Instance.BuildBundle(true, true);

        //BuildBundle.BuildAllAssetBundlesWithList ();
    }

    [MenuItem ("Tools/Build/Bundle(Log)")]
    public static void BuildABWithLog ()
    {
        BuildBundleConfig.Instance.BuildBundle(true, false);

        //BuildBundle.BuildAllAssetBundlesWithList ("", false);
    }
    private static void PlayerSetting_Common ()
    {
        PlayerSettings.companyName = "YB";
        PlayerSettings.productName = _product;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.useAnimatedAutorotation = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetApplicationIdentifier (_group, _identifier);
        PlayerSettings.bundleVersion = _version;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private static void PlayerSetting_SwitchPlatform (BuildTarget target, BuildTargetGroup group)
    {
        _target = target;
        _group = group;
        var t = EditorUserBuildSettings.selectedStandaloneTarget;
        var g = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (_target != t || _group != g)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget (_group, _target);
        }

    }

    private static void PlayerSetting_Win32 ()
    {
        PlayerSetting_SwitchPlatform (BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);
        PlayerSetting_Common ();
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1136;
        PlayerSettings.defaultScreenHeight = 640;
        _targetDir = Path.Combine (Application.dataPath.Replace ("/Assets", ""), "Win32");
        PlayerSettings.SetScriptingBackend (_group, ScriptingImplementation.Mono2x);
        PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.Standalone, _macro);
        PlayerSettings.SetApiCompatibilityLevel (_group, ApiCompatibilityLevel.NET_4_6);
        PlayerSettings.SetManagedStrippingLevel (BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);
        //PlayerSettings.strippingLevel = StrippingLevel.Disabled;
    }

    private static void PlayerSetting_iOS ()
    {
        PlayerSetting_SwitchPlatform (BuildTarget.iOS, BuildTargetGroup.iOS);
        PlayerSetting_Common ();
        _targetDir = Path.Combine (Application.dataPath.Replace ("/Assets", ""), "IOS");
        PlayerSettings.iOS.buildNumber = _version;
        PlayerSettings.SetScriptingBackend (BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.accelerometerFrequency = 0;
        PlayerSettings.iOS.locationUsageDescription = "";
        PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.iOS, _macro);
        PlayerSettings.SetApiCompatibilityLevel (_group, ApiCompatibilityLevel.NET_4_6);
        PlayerSettings.aotOptions = "nrgctx-trampolines=4096,nimt-trampolines=4096,ntrampolines=4096";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        // PlayerSettings.iOS.targetOSVersionString = "9.0";
        PlayerSettings.stripEngineCode = false;
        //PlayerSettings.strippingLevel = StrippingLevel.StripByteCode;
        PlayerSettings.SetManagedStrippingLevel (BuildTargetGroup.iOS, ManagedStrippingLevel.Disabled);
        PlayerSettings.iOS.scriptCallOptimization = ScriptCallOptimizationLevel.FastButNoExceptions;
    }

    private static void PlayerSetting_Android ()
    {
        PlayerSetting_SwitchPlatform (BuildTarget.Android, BuildTargetGroup.Android);
        PlayerSetting_Common ();
        _targetDir = Path.Combine (Application.dataPath.Replace ("/Assets", ""), "Android");
        int bundleVersionCode = int.Parse (System.DateTime.Now.ToString ("yyMMdd"));
        PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel18;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
        PlayerSettings.Android.forceSDCardPermission = true;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.SetScriptingDefineSymbolsForGroup (BuildTargetGroup.Android, _macro);
        PlayerSettings.SetScriptingBackend (BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel (_group, ApiCompatibilityLevel.NET_4_6);
        // PlayerSettings.strippingLevel = StrippingLevel.Disabled;
        PlayerSettings.SetManagedStrippingLevel (BuildTargetGroup.iOS, ManagedStrippingLevel.Disabled);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        PlayerSettings.Android.keystoreName = Application.dataPath + "/Editor/Platform/android.keystore";
        PlayerSettings.Android.keystorePass = "XCvis8RGbw";
        PlayerSettings.Android.keyaliasName = "yunstudio";
        PlayerSettings.Android.keyaliasPass = "XCvis8RGbw";
        PlayerSettings.Android.splashScreenScale = AndroidSplashScreenScale.ScaleToFill;
    }

    public static void Build ()
    {
        _scenes = FindEnabledEditorScenes ();
        EditorUserBuildSettings.SwitchActiveBuildTarget (_group, _target);
        if (Directory.Exists (_targetDir))
        {
            try { Directory.Delete (_targetDir, true); }
            catch (System.Exception e) { Debug.Log (e.Message); }
        }
        Directory.CreateDirectory (_targetDir);

        OnPriorBuild ();
        string lastName = "";
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.Android:
                lastName = ".apk";
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                lastName = ".exe";
                break;
        }
        string dest = Path.Combine (_targetDir, "ybgame" + lastName);
        var res = BuildPipeline.BuildPlayer (_scenes, dest, _target, _build);

        OnPostBuild ();
        AssetDatabase.Refresh ();
        EditorUtility.DisplayDialog ("Package Build Finish", "Package Build Finish!(" + res.summary + ")", "OK");
        HelperEditor.Open (_targetDir);
    }

    private static string[] FindEnabledEditorScenes ()
    {
        List<string> EditorScenes = new List<string> ();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            //if (scene.enabled && !EditorScenes.Contains(scene.path))
            //if (scene.path.Contains ("entrance.unity") ||
            //    scene.path.Contains ("empty.unity"))
                EditorScenes.Add (scene.path);
        }
        return EditorScenes.ToArray ();
    }

    public static void SwitchPlatForm (TPlatform platformType)
    {
        switch (platformType)
        {
            case TPlatform.Win32:
                PlayerSetting_Win32 ();
                break;
            case TPlatform.iOS:
                PlayerSetting_iOS ();
                break;
            case TPlatform.Android:
                PlayerSetting_Android ();
                break;
            default:
                Debug.Log ("Unknown platform " + platformType);
                break;
        }
//#if _USE_DEV_BUILD
        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.connectProfiler = true;
//#else
//        EditorUserBuildSettings.development = false;
//        EditorUserBuildSettings.connectProfiler = false;
//#endif
        AssetDatabase.SaveAssets ();
        AssetDatabase.Refresh ();
    }

    private static bool OnPriorBuild ()
    {
        StreammingFileBuild.ProcessStreamingFiles (_target);
        //if (_target == BuildTarget.Android)
        //{
        //    Dll2Bytes (false);
        //    TextAsset data = Resources.Load<TextAsset> ("YBTest");
        //    return data != null && data.bytes.Length > 0;
        //}
        if (_target == BuildTarget.iOS)
        {
            iOS_ECS (false);
        }
        return true;
    }

    private static void OnPostBuild ()
    {
        //if (_target == BuildTarget.Android)
        //{
        //    Dll2Bytes (true);
        //}
        if (_target == BuildTarget.iOS)
        {
            iOS_ECS (true);
        }
    }

    private static void iOS_ECS (bool reverse)
    {
        string dst = "Assets/Lib/XEcsGamePlay.dll";
        string src = "Assets/Lib/XEcsGamePlay.dll.iOS";
        string mdb = "Assets/Lib/XEcsGamePlay.dll.mdb";
        string tmp = "Assets/Lib/XEcsGamePlay.dll.bkup";
        AssetDatabase.DeleteAsset (mdb);
        if (reverse)
        {
            AssetDatabase.MoveAsset (dst, src);
            AssetDatabase.MoveAsset (tmp, dst);
            AssetDatabase.ImportAsset (src);
            AssetDatabase.ImportAsset (dst);
        }
        else
        {
            AssetDatabase.MoveAsset (dst, tmp);
            AssetDatabase.MoveAsset (src, dst);
            AssetDatabase.ImportAsset (tmp);
            AssetDatabase.ImportAsset (dst);
        }
        AssetDatabase.SaveAssets ();
        AssetDatabase.Refresh ();
    }

    private static void Dll2Bytes (bool reverse)
    {
        string src = "Assets/Lib/YBTest.dll";
        string dst = "Assets/Resources/YBTest.bytes";
        if (reverse)
        {
            AssetDatabase.MoveAsset (dst, src);
            AssetDatabase.ImportAsset (src);
        }
        else
        {
            AssetDatabase.MoveAsset (src, dst);
            AssetDatabase.ImportAsset (dst);
        }
        AssetDatabase.SaveAssets ();
        AssetDatabase.Refresh ();
    }
}