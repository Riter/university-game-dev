using System.Collections;
using UnityEngine;

public class EchoDoor : EchoPulseReactive
{
    public Transform doorTransform;
    public Vector3 openOffset = new Vector3(0f, 3f, 0f);
    public float moveSpeed = 4f;
    public float holdOpenDuration = 8f;
    public Collider blockingCollider;
    public Material closedMaterial;
    public Material openMaterial;
    public bool hideWhenClosed = true;

    private Vector3 closedLocalPosition;
    private Renderer[] renderers;
    private Coroutine doorRoutine;
    private Coroutine openVisualRoutine;

    private void Awake()
    {
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        closedLocalPosition = doorTransform.localPosition;
        renderers = GetComponentsInChildren<Renderer>();

        if (blockingCollider == null)
        {
            blockingCollider = GetComponentInChildren<Collider>();
        }

        ApplyClosedState();
    }

    private void OnEnable()
    {
        CacheComponents();
        ApplyClosedState();
    }

    private void OnValidate()
    {
        CacheComponents();

        if (!Application.isPlaying)
        {
            QueueEditorClosedState();
        }
    }

    public override void OnEchoPulse(Vector3 origin, float radius, float duration)
    {
        if (doorRoutine != null)
        {
            StopCoroutine(doorRoutine);
        }

        doorRoutine = StartCoroutine(OpenThenCloseRoutine(
            Mathf.Max(holdOpenDuration, duration),
            duration));
    }

    private IEnumerator OpenThenCloseRoutine(float openTime, float visibleTime)
    {
        ApplyOpenState();

        if (openVisualRoutine != null)
        {
            StopCoroutine(openVisualRoutine);
        }

        openVisualRoutine = StartCoroutine(OpenVisualRoutine(visibleTime));

        yield return MoveDoor(closedLocalPosition + openOffset);
        yield return new WaitForSeconds(openTime);
        yield return MoveDoor(closedLocalPosition);

        if (openVisualRoutine != null)
        {
            StopCoroutine(openVisualRoutine);
            openVisualRoutine = null;
        }

        ApplyClosedState();
        doorRoutine = null;
    }

    private IEnumerator OpenVisualRoutine(float visibleTime)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, visibleTime));
        ApplyDarkVisualState();
        openVisualRoutine = null;
    }

    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        while ((doorTransform.localPosition - targetPosition).sqrMagnitude > 0.0001f)
        {
            doorTransform.localPosition = Vector3.MoveTowards(
                doorTransform.localPosition,
                targetPosition,
                moveSpeed * Time.deltaTime);

            yield return null;
        }

        doorTransform.localPosition = targetPosition;
    }

    private void ApplyOpenState()
    {
        SetRenderersEnabled(true);
        SetMaterial(openMaterial);

        if (blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }
    }

    private void ApplyClosedState()
    {
        ApplyDarkVisualState();

        if (blockingCollider != null)
        {
            blockingCollider.enabled = true;
        }
    }

    private void ApplyDarkVisualState()
    {
        SetRenderersEnabled(!hideWhenClosed);
        SetMaterial(closedMaterial);
    }

    private void CacheComponents()
    {
        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        renderers = GetComponentsInChildren<Renderer>(true);

        if (blockingCollider == null)
        {
            blockingCollider = GetComponentInChildren<Collider>(true);
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = enabled;
        }
    }

    private void QueueEditorClosedState()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += ApplyEditorClosedState;
#endif
    }

#if UNITY_EDITOR
    private void ApplyEditorClosedState()
    {
        if (this == null || Application.isPlaying)
        {
            return;
        }

        CacheComponents();
        ApplyClosedState();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

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
