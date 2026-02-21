using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Header("Base Intensity")]
    public float baseIntensity = 1f;

    [Header("Flicker Settings")]
    public float flickerAmplitude = 0.3f;
    public float flickerSpeed = 1f;

    [Header("Limits")]
    public float minIntensity = 0.2f;
    public float maxIntensity = 2f;

    private Light2D light2D;
    private float noiseOffset;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        noiseOffset = Random.Range(0f, 1000f); // evita sincronización
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(
            noiseOffset,
            Time.time * flickerSpeed
        );

        float intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmplitude;
        intensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);

        light2D.intensity = intensity;
    }
}