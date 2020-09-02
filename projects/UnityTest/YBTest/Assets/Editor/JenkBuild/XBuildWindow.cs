using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;

namespace XEditor
{
    public class XBuildWindow : EditorWindow
    {

        private bool enable_bugly = false;
        private bool enable_bundle = true;
        private JenkinsBuild.TPlatform platform = JenkinsBuild.TPlatform.Win32;

        [MenuItem("Tools/Build/BuildWindow")]
        static void AnimExportTool()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorUtility.DisplayDialog("warn", "You need to current scene first!", "ok");
            }
            else
            {
                EditorWindow.GetWindowWithRect(typeof(XBuildWindow), new Rect(0, 0, 600, 800), true, "Build");
            }
        }


        private void OnEnable()
        {
            enable_bundle = false;
            enable_bugly = false;
            platform = GetCurrPlatform();
        }

        private JenkinsBuild.TPlatform GetCurrPlatform()
        {
#if UNITY_ANDROID
             return JenkinsBuild.TPlatform.Android;
#elif UNITY_IOS || UNITY_IPHONE
            return JenkinsBuild.TPlatform.iOS;
#else
            return JenkinsBuild.TPlatform.Win32;
#endif
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            EditorGUILayout.LabelField("Buiding");
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Select Platform:");
            GUILayout.Space(4);
            platform = (JenkinsBuild.TPlatform)EditorGUILayout.EnumPopup(platform);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            enable_bugly = GUILayout.Toggle(enable_bugly, " BUGLY");
            enable_bundle = GUILayout.Toggle(enable_bundle, " BUNDLE");
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("Build"))
            {
                OnClickBuild();
            }
            GUILayout.EndVertical();
        }




        private void OnClickBuild()
        {
            if (platform != GetCurrPlatform())
            {
                if (EditorUtility.DisplayDialog("warn", "Sure?", "ok"))
                {
                    DoBuild();
                }
            }
            else
            {
                DoBuild();
            }
        }

        private void DoBuild()
        {
            BuildAB();
            BuildPackage();
        }

        private void BuildAB()
        {
            if (enable_bundle)
            {
                JenkinsBuild.BuildAB();
            }
        }

        private void BuildPackage()
        {
            switch (platform)
            {
                case JenkinsBuild.TPlatform.Android:
                    JenkinsBuild.BuildAndroid();
                    break;
                case JenkinsBuild.TPlatform.iOS:
                    JenkinsBuild.BuildIOS();
                    break;
                case JenkinsBuild.TPlatform.Win32:
                    JenkinsBuild.BuildWin32();
                    break;
                default:
                    //TO-DO
                    break;
            }
        }
    }
}