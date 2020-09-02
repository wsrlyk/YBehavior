#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ResourceLoader : YBTest.IResourceHelp
{
    public UnityEngine.Object LoadEditorResource(string path, Type t)
    {
        return AssetDatabase.LoadAssetAtPath(path, t);
    }
}
#endif