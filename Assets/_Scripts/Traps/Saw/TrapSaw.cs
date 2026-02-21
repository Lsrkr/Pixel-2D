using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapSaw : MonoBehaviour
{
   [SerializeField] SpriteRenderer sr;
   [SerializeField] Animator animator;
   [SerializeField] float moveSpeed = 3f;
   [SerializeField] Transform[] wayPoints;
   [SerializeField] float delay;
   
   public int wayPointIndex = 1;
   public int moveDirection = 1;
   bool canMove = true;

   void Start()
   {
      transform.position = wayPoints[0].position;
   }

   void Update()
   {
      animator.SetBool("active", canMove);
      if(!canMove) return;
      transform.position = Vector2.MoveTowards(transform.position, wayPoints[wayPointIndex].position, moveSpeed * Time.deltaTime);
      if (Vector2.Distance(transform.position, wayPoints[wayPointIndex].position) < 0.1f)
      {
         if (wayPointIndex == wayPoints.Length - 1 || wayPointIndex == 0)
         {
            moveDirection *= -1;
            //StartCoroutine(SawRoutine());
         }
         wayPointIndex += moveDirection;
         
      }
   }

   IEnumerator SawRoutine()
   {
      canMove = false;

      yield return new WaitForSeconds(delay);
      
      canMove = true;
      sr.flipX = !sr.flipX;
   }
}
