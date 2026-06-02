using UnityEngine;

public class FragmentVisualPulse : MonoBehaviour
{
    [SerializeField] private Light glow;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 1.7f;
    [SerializeField] private float pulseAmount = 0.12f;
    [SerializeField] private float pulseSpeed = 2.1f;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private float startIntensity;

    public void Configure(Light newGlow)
    {
        glow = newGlow;
    }

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;
        startIntensity = glow != null ? glow.intensity : 0f;
    }

    private void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localPosition = startLocalPosition + Vector3.up * bob;
        transform.localScale = startLocalScale * pulse;
        if (glow != null) glow.intensity = startIntensity * pulse;
    }
}
