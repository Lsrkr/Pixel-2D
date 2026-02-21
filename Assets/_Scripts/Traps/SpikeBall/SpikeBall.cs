using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeBall : MonoBehaviour
{
    [SerializeField] Rigidbody2D spikeRB;
    [SerializeField] float pushForce;

    void Start()
    {
        var pushVector = new Vector2(pushForce, 0);
        spikeRB.AddForce(pushVector, ForceMode2D.Impulse);
    }
}
