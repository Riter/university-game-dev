using System.Collections;
using UnityEngine;

public class EchoRevealable : EchoPulseReactive
{
    public Material idleMaterial;
    public Material revealedMaterial;
    public bool hideRendererWhenIdle;
    public bool keepColliderActive = true;
    public float revealDurationOverride = -1f;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Coroutine revealRoutine;

    private void Awake()
    {
        RefreshCachedComponents();
        ApplyIdleState();
    }

    private void OnEnable()
    {
        RefreshCachedComponents();
        ApplyIdleState();
    }

    private void OnValidate()
    {
        RefreshCachedComponents();

        if (!Application.isPlaying)
        {
            QueueEditorIdleState();
        }
    }

    public override void OnEchoPulse(Vector3 origin, float radius, float duration)
    {
        RefreshCachedComponents();

        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        float revealTime = revealDurationOverride > 0f ? revealDurationOverride : duration;
        revealRoutine = StartCoroutine(RevealRoutine(revealTime));
    }

    private IEnumerator RevealRoutine(float revealTime)
    {
        ApplyRevealedState();
        yield return new WaitForSeconds(revealTime);
        ApplyIdleState();
    }

    private void ApplyRevealedState()
    {
        SetRenderersEnabled(true);
        SetMaterial(revealedMaterial);
        SetCollidersEnabled(true);
    }

    private void ApplyIdleState()
    {
        SetRenderersEnabled(!hideRendererWhenIdle);
        SetMaterial(idleMaterial);
        SetCollidersEnabled(keepColliderActive);
    }

    private void RefreshCachedComponents()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    private void QueueEditorIdleState()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += ApplyEditorIdleState;
#endif
    }

#if UNITY_EDITOR
    private void ApplyEditorIdleState()
    {
        if (this == null || Application.isPlaying)
        {
            return;
        }

        RefreshCachedComponents();
        ApplyIdleState();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = enabled;
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = enabled;
        }
    }

    private void SetMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
        }
    }
}
