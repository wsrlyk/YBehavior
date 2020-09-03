using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YBTest;

public class Entrance : MonoBehaviour
{
    YBTest.Game game;
    float lastUpdateTime = 0f;
    //UnityEngine.UI.Text text;
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Debug.Log("Initing");
#if UNITY_EDITOR
        YBTest.InterfaceMgr.Instance.AttachInterface<YBTest.IResourceHelp>(YBTest.InterfaceMgr.ResourceHelperID, new ResourceLoader());
#endif

        game = new YBTest.Game();

        //text = GameObject.Find("Canvas/Text").GetComponent<UnityEngine.UI.Text>();
    }

    // Update is called once per frame
    void Update()
    {
        float time = Time.time;
        if (time - lastUpdateTime > 3.0f)
        {
            game.Update();
            lastUpdateTime = time;

            string ss = string.Empty;
            foreach (string s in LogMgr.Instance)
            {
                ss += s;
                ss += "\n";
            }

            //text.text = ss;
        }
    }

    void _Destroy()
    {
        UnityEngine.Debug.Log("Destroying");

        if (game != null)
        {
            game.Destroy();
            game = null;
        }
    }

    private void OnDisable()
    {
        _Destroy();
    }

    private void OnDestroy()
    {
        _Destroy();
    }
}
