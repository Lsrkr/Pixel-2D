using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Chicken : Enemy
{
    [Header("Chicken Settings")] 
    [SerializeField] float aggroDuration;
    [SerializeField] bool playerDetected;
    [SerializeField] float detectionRange;
    [SerializeField] bool canFlip;
    
    float aggroTimer;
    
    
    protected override void Update()
    {
        base.Update();
        animator.SetFloat("xVelocity", rb.velocity.x);
        
        aggroTimer -= Time.deltaTime;
        
        if (isDead) return;

        if (playerDetected)
        {
            canMove = true;
            aggroTimer = aggroDuration;
        }
        
        if (aggroTimer < 0) canMove = false;
        
        HandleMovement();
        HandleCollisions();

        if (isGrounded) HandleTurnAround();
    }

    void HandleTurnAround()
    {
        if (!isGroundInFrontDetected || isWallDetected)
        {
            Flip();
            canMove = false;
            rb.velocity = Vector2.zero;
        }
    }

    void HandleMovement()
    {
        if (!canMove) return;

        HandleFlip(player.transform.position.x);
        rb.velocity = new Vector2(speed * facingDirection, rb.velocity.y);
    }

    protected override void Flip()
    {
        base.Flip();
        canFlip = true;
    }

    protected override void HandleFlip(float xValue)
    {
        // Determinar si el jugador está a la izquierda o derecha del enemigo
        bool playerIsToTheLeft = xValue < transform.position.x;
    
        // Si el jugador está a la izquierda y el enemigo mira a la derecha, o viceversa
        if ((playerIsToTheLeft && facingDirection > 0) || (!playerIsToTheLeft && facingDirection < 0))
        {
            if (canFlip)
            {
                canFlip = false;
                Invoke(nameof(Flip), 0.3f);
            }
        }
    }


    protected override void HandleCollisions()
    {
        base.HandleCollisions();
        playerDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, detectionRange,whatIsPlayer);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + detectionRange * facingDirection, transform.position.y));

    }
}
