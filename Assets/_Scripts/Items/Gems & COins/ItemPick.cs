using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ItemPick : MonoBehaviour
{
    enum ItemType{Saphire_Pink, Ruby_Red, Coin_Yellow}

    [SerializeField] ItemType itemType;
    [SerializeField] GameObject itemVFX;
    [SerializeField] TextMeshProUGUI textCollectible;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        //gameObject.SetActive(false);
        var newVFX = Instantiate(itemVFX, transform.position, Quaternion.identity);
        IncreaseCollectible(1);
        AudioAlchemist.Instance.PlayOneShotRandom("Collectibles");
        Destroy(newVFX, 1.5f);
        Destroy(gameObject);
    }


    void IncreaseCollectible(int amount)
    {
        textCollectible.text = amount.ToString();
    }
    
}
