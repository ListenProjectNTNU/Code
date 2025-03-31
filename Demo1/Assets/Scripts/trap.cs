using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    public float trapDamage = 15f;  // 地刺傷害
    public float damageCooldown = 1f; // 傷害間隔 (秒)
    private Dictionary<GameObject, float> lastDamageTime = new Dictionary<GameObject, float>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null && CanTakeDamage(player.gameObject))
            {
                ApplyDamage(player);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null && CanTakeDamage(player.gameObject))
            {
                ApplyDamage(player);
            }
        }
    }

    private void ApplyDamage(PlayerController player)
    {
        PlayerUtils.TakeDamage(player.healthBar, trapDamage - player.curdefence ); // 讓 `PlayerController` 自己計算防禦影響
        lastDamageTime[player.gameObject] = Time.time; // 更新傷害時間
    }

    private bool CanTakeDamage(GameObject player)
    {
        if (!lastDamageTime.ContainsKey(player) || Time.time - lastDamageTime[player] >= damageCooldown)
        {
            return true;
        }
        return false;
    }
}
