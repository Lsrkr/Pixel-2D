using UnityEngine;


public class MaterialGlowBounce : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private float minGlow = 0f;
    [SerializeField] private float maxGlow = 2f;
    [SerializeField] private float speed = 2f;

    [SerializeField] Material materialInstance;
    private float timer;

    private static readonly int GlowID = Shader.PropertyToID("_Glow");

    private void Update()
    {
        timer += Time.deltaTime * speed;

        float t = Mathf.PingPong(timer, 1f);
        float glowValue = Mathf.Lerp(minGlow, maxGlow, t);

        materialInstance.SetFloat(GlowID, glowValue);
    }
}

