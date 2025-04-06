using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;
    public string targetSceneName;

    // Update is called once per frame

    public void LoadNextLevel()
    {
        SceneManager.LoadScene(targetSceneName);    
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        //play animation
        transition.SetTrigger("Start");

        //wait
        yield return new WaitForSeconds(transitionTime);

        //load next scene
        SceneManager.LoadScene(levelIndex);
    }
}
