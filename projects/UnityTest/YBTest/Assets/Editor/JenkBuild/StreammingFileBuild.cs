using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public class StreammingFileBuild
{
    // 打包之前 打ab之后执行
    public static void ProcessStreamingFiles (BuildTarget target)
    {
        //BuildLua.Build();
        ProcessNative ();
        Flush ();
    }

    // private static void ProcessSkillPackage ()
    // {
    //     SkillEditor.PreBuildScript ();
    //     BehitEditor.PreBuildScript ();
    // }

    private static void ProcessNative ()
    {
        try
        {
            //File.Copy (@"Assets/Plugins/Ecs/Xuthus.dll.Native",
            //    @"Assets/Plugins/Ecs/Xuthus.dll", true);
            //AssetDatabase.ImportAsset (@"Assets/Plugins/Ecs/Xuthus.dll");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning (e.Message);
        }
    }


    private static void Flush ()
    {
        AssetDatabase.SaveAssets ();
        AssetDatabase.Refresh ();
    }

}