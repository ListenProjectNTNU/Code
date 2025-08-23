using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;
    public string targetSceneName;

    // Update is called once per frame

    public void LoadNextLevel()
    {
        if (DataPersistenceManager.instance != null)
            DataPersistenceManager.instance.LoadSceneAndUpdate(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);   
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        //play animation
        transition.SetTrigger("Start");

        //wait
        yield return new WaitForSeconds(transitionTime);

        //load next scene
        if (DataPersistenceManager.instance != null)
        {
            string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(levelIndex));
            DataPersistenceManager.instance.LoadSceneAndUpdate(sceneName);
        }
        else
        {
            SceneManager.LoadScene(levelIndex);
        }
    }
}
