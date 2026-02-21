using System;
using UnityEngine;
using UnityEngine.VFX;

public class Health : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] int maximumHealth;
    
    [Header("Components")] 
    public PlayerMovement playerMovement;

    int health;

    void OnEnable()
    {
        AdjustHealth();
        AdjustComponents();
    }

    public virtual void AdjustComponents()
    {
    //    sr.enabled = true;
    //    col.enabled = true;
    }

    public void Damage(int damageAmount)
    {
        health -= damageAmount;
        health = Mathf.Clamp(health, 0, maximumHealth);

        if (IsDead()) DestroyObject();
        else DamageObject();
    }

    /// <summary>
    /// Adjust health to be maximum health everytime the object is enabled
    /// </summary>
    void AdjustHealth() => health = maximumHealth;
    
    /// <summary>
    /// Return the amount of health
    /// </summary>
    bool IsDead() => health <= 0;

    public virtual void DestroyObject()
    {
      //  sr.enabled = false;
      //  col.enabled = false;
      //  rb.velocity = Vector2.zero;
      //  rb.angularVelocity = 0f;
    }

    public virtual void DamageObject()
    {
        
    }
}