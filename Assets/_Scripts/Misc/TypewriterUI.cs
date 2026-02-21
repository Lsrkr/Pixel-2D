using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TypewriterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI textUI;
    [SerializeField] private Image[] imagesToFade;

    [Header("Text Settings")]
    [TextArea]
    [SerializeField] private string fullText;
    [SerializeField] private float typingSpeed = 40f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 2f;

    private bool started = false;

    private void Start()
    {
        textUI.text = "";

        // Todas las imágenes empiezan invisibles
        SetImagesAlpha(0f);
    }

    private void Update()
    {
        if (!started && Input.GetKeyDown(KeyCode.LeftControl))
        {
            started = true;

            StartCoroutine(TypeText());
            StartCoroutine(FadeInImages());
        }
    }

    IEnumerator TypeText()
    {
        textUI.text = fullText;
        textUI.ForceMeshUpdate();

        int totalCharacters = textUI.textInfo.characterCount;
        textUI.maxVisibleCharacters = 0;

        float timer = 0f;

        while (textUI.maxVisibleCharacters < totalCharacters)
        {
            timer += Time.deltaTime * typingSpeed;
            textUI.maxVisibleCharacters = Mathf.FloorToInt(timer);

            yield return null;
        }

        textUI.maxVisibleCharacters = totalCharacters;
    }

    IEnumerator FadeInImages()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / fadeDuration);
            SetImagesAlpha(t);

            yield return null;
        }

        SetImagesAlpha(1f);
    }

    private void SetImagesAlpha(float value)
    {
        for (int i = 0; i < imagesToFade.Length; i++)
        {
            if (imagesToFade[i] == null) continue;

            Color c = imagesToFade[i].color;
            c.a = value;
            imagesToFade[i].color = c;
        }
    }
}

