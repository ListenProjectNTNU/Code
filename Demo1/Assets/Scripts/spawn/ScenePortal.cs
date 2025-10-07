using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ScenePortal : MonoBehaviour {
    [Header("Next")]
    public string nextSceneName;
    public string nextSpawnId = "default";

    [Header("Optional")]
    public bool requirePlayerInDialogueIdle = false;

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;

        if (requirePlayerInDialogueIdle) {
            var dm = DialogueManager.GetInstance(); // 若你有 INK
            if (dm != null && dm.dialogueIsPlaying) return;
        }

        GameManager.I.GoToScene(nextSceneName, nextSpawnId);
    }
}
