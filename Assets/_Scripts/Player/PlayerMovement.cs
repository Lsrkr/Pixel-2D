using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;
    [SerializeField] Animator animator;
    [SerializeField] PlayerHealth playerHealth;

    [Header("Player Properties")]
    [SerializeField] float moveSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float doubleJumpForce;

    [Header("Interaction Properties")] 
    [SerializeField] float wallJumpDuration;
    [SerializeField] Vector2 wallJumpForce;
    bool isWallJumping;
    
    [Header("Knockback Properties")]
    [SerializeField] float knockbackDuration;
    [SerializeField] Vector2 knockbackForce;
    bool isKnockbacking;
    
    [Header("Buffer & Coyote Jump")]
    [SerializeField] float bufferJumpWindow;
    [SerializeField] float bufferJumpPressed;
    [SerializeField] float coyoteJump = .5f;
    float coyoteJumpActivated = -1f;
    
    [Header("Collision info")]
    [SerializeField] float groundCheckRadius;
    [SerializeField] float wallCheckRadius;
    [SerializeField] LayerMask groundMask;
    
    [Header("Enemy Collisions")]
    [SerializeField] LayerMask whatIsEnemy;
    [SerializeField] Transform enemyCheck;
    [SerializeField] float enemyCheckRadius;
    
    bool isGrounded;
    bool isRunning;
    float xInput;
    float yInput;
    bool flip = true;
    int facingDirection = 1;
    bool canDoubleJump;
    bool isAirborne;
    public bool isWallDetected;
    bool canBeControlled = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        AirborneStatus();

        if (!canBeControlled)
        {
            HandleMovementAnimation();
            HandleCollisions();
            return;
        }
        
        if(isKnockbacking) return;

        HandleEnemyDetection();
        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollisions();
        HandleMovementAnimation();
    }

    public void Knockback(float sourceDamage)
    {
        var knobackDirection = 1f;

        if (transform.position.x < sourceDamage)
        {
            knobackDirection = -1;
        }
        
        if(isKnockbacking) return;
        StartCoroutine(KnockbackRoutine());
        rb.velocity = new Vector2(knockbackForce.x * knobackDirection, knockbackForce.y);
    }

    void HandleWallSlide()
    {
        var canWallSlide = isWallDetected && rb.velocity.y < 0;
        
        var yModifier = yInput < 0 ? 1 : 0.5f;
        
        if (!canWallSlide) return;

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifier);
    }

    void AirborneStatus()
    {
        if (isGrounded && isAirborne) HandleLanding();
        if (!isGrounded && !isAirborne) BecomeAirborne();
    }

    void BecomeAirborne()
    {
        isAirborne = true;
        if(rb.velocity.y < 0) ActivateCoyoteJump();
    }

    void HandleLanding()
    {
        isAirborne = false;
        canDoubleJump = true;
        AttemptBufferJump();
    }

    void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpButton();
            RequestBufferJump();
        }
    }

    void RequestBufferJump()
    {
        if(isAirborne) bufferJumpPressed =Time.time;
    }

    void AttemptBufferJump()
    {
        if (Time.time < bufferJumpPressed + bufferJumpWindow)
        {
            bufferJumpPressed = Time.time -1;
            Jump();
        }
    }

    void ActivateCoyoteJump()
    {
        coyoteJumpActivated = Time.time;
    }
    
    void CancelCoyoteJump() => coyoteJumpActivated = Time.time -1;
    

    void JumpButton()
    {
        bool coyoteJumpAvailable = Time.time < coyoteJumpActivated + coyoteJump;
        if (isGrounded || coyoteJumpAvailable)
        {
            Jump();
            AudioAlchemist.Instance.Play(AudioID.Jump);
        }
        else if (isWallDetected && !isGrounded) WallJump();
        else if (canDoubleJump)
        {
            DoubleJump();
            AudioAlchemist.Instance.Play(AudioID.Jump);
        }
        CancelCoyoteJump();
    }

    void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpForce);

    void DoubleJump()
    {
        isWallJumping = false;
        canDoubleJump = false;
        rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);
    }

    void WallJump()
    {
        canDoubleJump = true;
        rb.velocity = new Vector2(wallJumpForce.x * -facingDirection, wallJumpForce.y);
        SpriteFlip();
        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
        AudioAlchemist.Instance.Play(AudioID.Jump);
    }

    IEnumerator WallJumpRoutine()
    {
        isWallJumping = true;
        
        yield return new WaitForSeconds(wallJumpDuration);
        
        isWallJumping = false;
    }

    IEnumerator KnockbackRoutine()
    {
        isKnockbacking = true;
        animator.SetBool("knock", true);
        
        yield return new WaitForSeconds(knockbackDuration);
        
        isKnockbacking = false;
        animator.SetBool("knock", false);
        
        playerHealth.ResetDamage();
    }

    void HandleCollisions()
    {
        isGrounded = Physics2D.Raycast(transform.position,Vector2.down, groundCheckRadius, groundMask);
        isWallDetected = Physics2D.Raycast(transform.position,Vector2.right * facingDirection,wallCheckRadius ,groundMask);        
    }

    void HandleMovement()
    {
        if(isWallDetected) return;
        if(isWallJumping) return;
        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    void HandleMovementAnimation()
    {
        animator.SetFloat("xVelocity", rb.velocity.x);
        animator.SetFloat("yVelocity", rb.velocity.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWallDetected", isWallDetected);
    }

    void HandleFlip()
    {
        if (xInput < 0 && flip || xInput > 0 && !flip) SpriteFlip();
    }

    void SpriteFlip()
    {
        facingDirection *= -1;
        transform.Rotate(0f, 180f, 0f);
        flip = !flip;
    }

    public void PlayerPushTrampoline(Vector2 direction, float duration = 0)
    {
        StartCoroutine(PushCoroutine(direction, duration));
    }

    IEnumerator PushCoroutine(Vector2 direction, float duration)
    {
        canBeControlled = false;
        rb.velocity = Vector2.zero;
        rb.AddForce(direction, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(duration);
        
        canBeControlled = true;
    }

    void HandleEnemyDetection()
    {
        if (rb.velocity.y >= 0) return;
        var colliders = Physics2D.OverlapCircleAll(enemyCheck.position, enemyCheckRadius, whatIsEnemy);
        foreach (var enemy in colliders)
        {
            var newEnemy = enemy.GetComponent<Enemy>();
            if (newEnemy != null) newEnemy.Die();
                Jump();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(enemyCheck.position, enemyCheckRadius);
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckRadius));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + wallCheckRadius * facingDirection, transform.position.y));
    }
}
