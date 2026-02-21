using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFootSteps : MonoBehaviour
{
    public void FootSteps() => AudioAlchemist.Instance.PlayOneShotRandom("Walking");
}
