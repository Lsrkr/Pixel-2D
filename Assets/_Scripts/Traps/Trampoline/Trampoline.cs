using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [SerializeField] float pushPower;
    public Animator animator;
    [SerializeField] Vector2 pushDirection;
    [SerializeField] float duration = .5f;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var playerPush = other.gameObject.GetComponent<PlayerMovement>();
            playerPush.PlayerPushTrampoline(transform.up * pushPower, duration);
            animator.SetTrigger("activate");
        }
    }
}
