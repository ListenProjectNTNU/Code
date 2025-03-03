using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{

	public Transform player;

	public bool isFlipped = false;

	void Start()
	{
		if (player == null) // 檢查是否已經手動指派
		{
			player = GameObject.FindGameObjectWithTag("Player")?.transform;
			
			if (player == null)
			{
				Debug.LogError("Boss 找不到 Player，請確認場景中的 Player 物件標籤是否設為 'Player'！");
			}
		}
	}
	public void LookAtPlayer()
	{
		Vector3 flipped = transform.localScale;
		flipped.z *= -1f;

		if (transform.position.x > player.position.x && isFlipped)
		{
			transform.localScale = flipped;
			transform.Rotate(0f, 180f, 0f);
			isFlipped = false;
		}
		else if (transform.position.x < player.position.x && !isFlipped)
		{
			transform.localScale = flipped;
			transform.Rotate(0f, 180f, 0f);
			isFlipped = true;
		}
	}

}
