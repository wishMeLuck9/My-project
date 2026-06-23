using TMPro;
using UnityEngine;

public class MenuTitleFlicker : MonoBehaviour
{
    [SerializeField] private TMP_Text primary;
    [SerializeField] private TMP_Text ghost;
    [SerializeField] private float flickerSpeed = 1.2f;
    [SerializeField] private float ghostJitter = 1.5f;
    [SerializeField] private Color primaryColor = new Color(0.9f, 0.96f, 0.88f, 1f);
    [SerializeField] private Color ghostColor = new Color(0.22f, 0.52f, 0.34f, 0.28f);

    private Vector3 ghostBasePosition;

    private void Awake()
    {
        if (primary == null) primary = GetComponent<TMP_Text>();
        if (ghost != null) ghostBasePosition = ghost.rectTransform.localPosition;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 7.31f);
        if (primary != null)
        {
            Color color = primaryColor;
            color.a = Mathf.Lerp(0.86f, 1f, noise);
            primary.color = color;
        }

        if (ghost != null)
        {
            Color color = ghostColor;
            color.a = Mathf.Lerp(0.12f, 0.34f, noise);
            ghost.color = color;
            ghost.rectTransform.localPosition = ghostBasePosition + new Vector3((noise - 0.5f) * ghostJitter, 0f, 0f);
        }
    }
}
