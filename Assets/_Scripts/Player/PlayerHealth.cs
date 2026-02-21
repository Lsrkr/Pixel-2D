using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : Health
{
    bool damaged;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Hazard"))
        {
            if (damaged) return;
            damaged = true;
            Damage(1);
            playerMovement.Knockback(transform.position.x);
        }
    }

    public override void DestroyObject()
    {
        GameManager.Instance.LostHearths();
    }

    public override void DamageObject()
    {
        GameManager.Instance.LostHearths();
    }
    
    public void ResetDamage() => damaged = false;
}
