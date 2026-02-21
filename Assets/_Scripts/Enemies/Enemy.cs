using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody2D rb;

    [Header("Settings")] 
    [SerializeField] protected Transform player;
    [SerializeField] protected float idleDuration;
    protected float idleTimer;
    [SerializeField] protected float speed;
    [SerializeField] protected float groundCheckDistance = 1.1f;
    [SerializeField] protected float wallCheckDistance = .7f;
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected LayerMask whatIsPlayer;

    [Header("Death Details")] 
    [SerializeField] float deathImpact;
    [SerializeField] float deathRotationSpeed;
    [SerializeField] protected GameObject damageTrigger;
    [SerializeField] Collider2D collider;
    
    
    protected bool canMove = true;
    protected bool isDead;
    int deathRotationDirection = 1;
    protected bool isGrounded;
    protected bool isWallDetected;
    protected bool isGroundInFrontDetected;
    protected bool flip = true;
    protected float xInput;
    protected int facingDirection = -1;

    protected void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    

    protected virtual void Update()
    {
        idleTimer -= Time.deltaTime;
        if (isDead) HandleDeathRotation();
    }

    protected virtual void HandleCollisions()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isGroundInFrontDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, wallCheckDistance, whatIsGround);
    }

    protected virtual void HandleFlip(float xValue)
    {
        if (xValue < transform.position.x && flip || xValue > transform.position.x && !flip) Flip();
    }

    protected virtual void Flip()
    {
        facingDirection *= -1;
        transform.Rotate(0f, 180f, 0f);
        flip = !flip;
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(groundCheck.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(groundCheck.position, new Vector2(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + wallCheckDistance * facingDirection, transform.position.y));
    }

    public virtual void Die()
    {
        damageTrigger.SetActive(false);
        collider.enabled = false;
        animator.SetTrigger("hit");
        isDead = true;
        rb.velocity = new Vector2(rb.velocity.x, deathImpact);
        if (Random.Range(0, 100) < 50) deathRotationDirection *= -1;
    }

    void HandleDeathRotation()
    {
        transform.Rotate(0,0, deathRotationSpeed * deathRotationDirection *Time.deltaTime);
    }
}
