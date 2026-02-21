using UnityEngine;
using Random = UnityEngine.Random;

public class FallingPlatform : MonoBehaviour
{
    [Header("Falling Platform Components")]
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] BoxCollider2D[] colliders;
    [SerializeField] Vector3 defaultPosition;
    
    [Header("Falling Platform Settings")]
    [SerializeField] float speed = 2.5f;
    [SerializeField] float traveledDistance;
    [SerializeField] float impactSpeed = 3f;
    [SerializeField] float impactDuration;
    [SerializeField] float fallDelay;

    bool impacted;
    float impactTimer;
    bool canMove;
    Vector3[] wayPoints;
    int wayPointIndex;
    bool hideFallingPlatform;
    
    
    void OnEnable()
    {
        SetupWayPoints();
        var randomDelay = Random.Range(0f, .6f);
        Invoke("ActivePlatform", randomDelay);
        animator.Play("On");
    }

    void OnDisable()
    {
        transform.position = defaultPosition;
        hideFallingPlatform = false;
        impactTimer = impactDuration;
        impacted = false;
        rb.isKinematic = true;
        CollidersStatus(true);
    }

    void ActivePlatform() => canMove = true;

    void Update()
    {
        HandleImpact();
        HandleMovement();
        HidePlatform();
    }

    void SetupWayPoints()
    {
        wayPoints = new Vector3[2];

        var yOffset = traveledDistance / 2;
        
        wayPoints[0] = transform.position + new Vector3(0, yOffset, 0);
        wayPoints[1] = transform.position + new Vector3(0, -yOffset, 0);
    }

    void HandleMovement()
    {
        if(!canMove) return;
        transform.position = Vector3.MoveTowards(transform.position, wayPoints[wayPointIndex],
            speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, wayPoints[wayPointIndex]) < .1f)
        {
            wayPointIndex++;
            if (wayPointIndex >= wayPoints.Length)
            {
                wayPointIndex = 0;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted) return;
        if (other.CompareTag("Player"))
        {
            Invoke(nameof(SwitchOffPlatform), fallDelay);
            impactTimer = impactDuration;
            impacted = true;
        }
    }

    void SwitchOffPlatform()
    {
        canMove = false;
        rb.isKinematic = false;
        animator.SetTrigger("activate");
        rb.gravityScale = 3.5f;
        CollidersStatus(false);
    }

    void CollidersStatus(bool status)
    {
        foreach (var col in colliders)
        {
            col.enabled = status;
        }
    }

    void HandleImpact()
    {
        if(impactTimer < 0) return;
        impactTimer -= Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position,
            transform.position + Vector3.down * 10, impactSpeed * Time.deltaTime);
    }

    void HidePlatform()
    {
        if (!(transform.position.y < -20f)) return;
        if (hideFallingPlatform) return;
        hideFallingPlatform = true;
        TrapManager.Instance.FallingPlatform(gameObject);
    }
}
