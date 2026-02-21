using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapArrow : Trampoline
{
    [Header("Arrow settings")]
    [SerializeField] float rotationSpeed;
    [SerializeField] bool rotationRight;
    [SerializeField] float growSpeed;
    [SerializeField] Vector3 targetScale;
    
    
    int direction = -1;

    void OnEnable()
    {
        rotationRight = !rotationRight;
        animator.Play("arrow");
        transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
    }

    void Update()
    {
        if (transform.localScale.x < targetScale.x)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * growSpeed);
        }
        HandleRotation();
    }

    void HandleRotation()
    {
        direction = rotationRight ? -1 : 1;

        transform.Rotate(0,0, rotationSpeed * direction *Time.deltaTime);
    }

    void Cooldown()
    {
        TrapManager.Instance.ArrowCooldown(gameObject);
    }
}
