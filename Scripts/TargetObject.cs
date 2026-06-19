using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TargetObject : MonoBehaviour
{
    [Header("Visuals")]
    public Color idleColor    = new Color(1f, 0.5f, 0f);   // orange
    public Color glowColor    = new Color(1f, 1f, 0.2f);   // yellow
    public Color successColor = new Color(0.2f, 1f, 0.3f); // green
    public float pulsePeriod  = 1.2f;
    public float glowIntensity = 2f;

    public bool IsGrasped { get; private set; } = false;

    private Renderer   _rend;
    private Material   _mat;
    private Coroutine  _pulse;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        if (_rend != null)
        {
            _mat = _rend.material;
            _mat.EnableKeyword("_EMISSION");
        }

        // Make rigidbody kinematic so the sphere floats in place
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_grab != null)
            _grab.selectEntered.AddListener(OnGrasped);
    }

    public void Activate()
    {
        IsGrasped = false;
        gameObject.SetActive(true);
        _pulse = StartCoroutine(PulseGlow());
    }

    void OnGrasped(SelectEnterEventArgs args)
    {
        if (IsGrasped) return;
        IsGrasped = true;

        if (_pulse != null) StopCoroutine(_pulse);
        SetColor(successColor);
        StartCoroutine(DelayDestroy(0.5f));
    }

    IEnumerator PulseGlow()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float alpha = (Mathf.Sin(t * Mathf.PI * 2f / pulsePeriod) + 1f) * 0.5f;
            SetColor(Color.Lerp(idleColor, glowColor, alpha), alpha);
            yield return null;
        }
    }

    void SetColor(Color c, float emissionAlpha = 1f)
    {
        if (_mat == null) return;
        _mat.color = c;
        _mat.SetColor(EmissionColorID, c * (glowIntensity * emissionAlpha));
    }

    IEnumerator DelayDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_grab != null) _grab.selectEntered.RemoveListener(OnGrasped);
    }
}
