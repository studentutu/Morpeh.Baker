using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    private int currentScene
    {
        get
        {
            return this.gameObject.scene.buildIndex;
        }
    }

    public void ChangeSceneNext()
    {
        if ((currentScene + 1) >= ScenesContainer.Instance.AllScenes)
        {
            return;
        }
        var async = SceneManager.LoadSceneAsync(currentScene + 1, LoadSceneMode.Single);
        async.allowSceneActivation = true;
    }

    public void ChangeScenePrev()
    {
        if ((currentScene - 1) < 0)
        {
            return;
        }
        var async = SceneManager.LoadSceneAsync(currentScene - 1, LoadSceneMode.Single);
        async.allowSceneActivation = true;
    }
}
