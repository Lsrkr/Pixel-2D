using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Mushroom : Enemy
{
    protected override void Update()
    {
        base.Update();
        animator.SetFloat("xVelocity", rb.velocity.x);
        if (isDead) return;
        HandleMovement();
        HandleCollisions();

        if (isGrounded) HandleTurnAround();
    }

    void HandleTurnAround()
    {
        if (!isGroundInFrontDetected || isWallDetected)
        {
            Flip();
            idleTimer = idleDuration;
            rb.velocity = Vector2.zero;
        }
    }

    void HandleMovement()
    {
        if (idleTimer > 0) return;
        rb.velocity = new Vector2(speed * facingDirection, rb.velocity.y);
    }
}
