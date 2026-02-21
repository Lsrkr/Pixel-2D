using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapManager : MonoBehaviour
{
   public static TrapManager Instance;
   [Header("Arrow")] 
   [SerializeField] float arrowCooldownTimer;
   
   [Header("Falling Trap")]
   [SerializeField] float fallingTrapTimer;
   

   void Awake()
   {
      Instance = this;
   }


   public void ArrowCooldown(GameObject arrow)
   {
      StartCoroutine(ArrowCooldownRoutine(arrow));
   }

   IEnumerator ArrowCooldownRoutine(GameObject arrow)
   {
      arrow.SetActive(false);
      yield return new WaitForSeconds(arrowCooldownTimer);
      arrow.SetActive(true);
   }

   public void FallingPlatform(GameObject fallingPlatform) => StartCoroutine(FallingPlatformRoutine(fallingPlatform));

   IEnumerator FallingPlatformRoutine(GameObject fallingPlatform)
   {
      fallingPlatform.SetActive(false);
      yield return new WaitForSeconds(arrowCooldownTimer);
      fallingPlatform.SetActive(true);
   }
   
}
