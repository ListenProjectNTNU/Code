using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootBag : MonoBehaviour
{
    public GameObject droppedItemPrefab;
    public List<Loot> lootList = new List<Loot>();
    
    //選定掉落物
    private Loot GetDroppedItem()
    {
        int randomNumber = Random.Range(1,101);
        List<Loot> possibleItems = new List<Loot>();
        foreach (Loot item in lootList)
        {
            if(randomNumber <= item.dropChance)
            {
                possibleItems.Add(item);
            }
        }
        if(possibleItems.Count > 0)
        {
            Loot droppedItem = possibleItems[Random.Range(0,possibleItems.Count)];
            return droppedItem;
        }
        Debug.Log("No loot drop");
        return null;
    }

    //執行掉落
    public void InstantiateLoot(Vector3 spawnPosition)
    {
        //Debug.Log("drop");
        Loot droppedItem = GetDroppedItem();
        if(droppedItem != null)
        {
            //加上隨機偏移，讓掉落物稍微分散
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, 0f);
            Vector3 spawnPosWithOffset = spawnPosition + offset;

            GameObject lootGameObject = Instantiate(droppedItemPrefab, spawnPosWithOffset, Quaternion.identity);
            lootGameObject.GetComponent<SpriteRenderer>().sprite = droppedItem.lootSprite;

            lootGameObject.AddComponent<LootItem>().lootData = droppedItem;
            //如果要加掉落動畫可以在這裡加
        }
    }
}
