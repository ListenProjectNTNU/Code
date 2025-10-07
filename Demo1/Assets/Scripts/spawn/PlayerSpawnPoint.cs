using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour {
    public string spawnId = "default";
    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.25f, $"Spawn: {spawnId}");
    }
}
