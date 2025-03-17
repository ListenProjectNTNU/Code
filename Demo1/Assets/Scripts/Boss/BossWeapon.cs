using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWeapon : MonoBehaviour
{
	public int attackDamage = 20;
	public int enragedAttackDamage = 40;

	public Vector3 attackOffset;
	public float attackRange = 1f;
	public LayerMask attackMask;

	public void Attack()
	{
		Vector3 pos = transform.position;
		pos += transform.right * attackOffset.x;
		pos += transform.up * attackOffset.y;

		Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
		if (colInfo != null)
		{
			// 嘗試從 Player 取得 PlayerController
			PlayerController player = colInfo.GetComponent<PlayerController>();
			if (player != null)
			{
				// 使用 PlayerController 中的 Health Bar
				if (player.healthBar != null)
				{
					//player.healthBar.SetHealth(player.healthBar.currenthp - 100);
					PlayerUtils.TakeDamage(player.healthBar, 20f);
					Debug.Log("成功對 Player 造成傷害！");
				}
				else
				{
					Debug.LogWarning("Player 的 Health Bar 為空！");
				}
			}
			else
			{
				Debug.LogWarning("碰到的物体没有 PlayerController 组件：" + colInfo.name);
			}
		}
	}

	public void EnragedAttack()
	{
		Vector3 pos = transform.position;
		pos += transform.right * attackOffset.x;
		pos += transform.up * attackOffset.y;

		Collider2D colInfo = Physics2D.OverlapCircle(pos, attackRange, attackMask);
		if (colInfo != null)
		{
			// 取得 Player 的 healthbar
			PlayerController player = colInfo.GetComponent<PlayerController>();
			if (player.healthBar != null)
			{
				PlayerUtils.TakeDamage(player.healthBar, 100); // ✅ 正確使用 PlayerUtils
				Debug.Log("成功對 Player 造成傷害！");
			}
			else
			{
				Debug.LogWarning("找到 Player，但沒有 healthbar：" + colInfo.name);
			}
		}
	}

	void OnDrawGizmosSelected()
	{
		Vector3 pos = transform.position;
		pos += transform.right * attackOffset.x;
		pos += transform.up * attackOffset.y;

		Gizmos.DrawWireSphere(pos, attackRange);
	}
}
