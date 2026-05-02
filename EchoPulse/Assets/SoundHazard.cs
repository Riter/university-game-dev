using System.Collections;
using UnityEngine;

public class SoundHazard : EchoPulseReactive
{
    public float awakeDuration = 2.8f;
    public Material sleepingMaterial;
    public Material awakeMaterial;
    public Collider triggerCollider;
    public bool hideWhenSleeping = true;
    public float resetCooldown = 0.5f;

    private Renderer[] renderers;
    private Coroutine awakeRoutine;
    private bool isAwake;
    private float nextAllowedResetTime;

    public bool IsAwake
    {
        get { return isAwake; }
    }

    private void Awake()
    {
        CacheComponents();
        SetAwakeState(false);
    }

    private void OnEnable()
    {
        CacheComponents();
        SetAwakeState(false);
    }

    private void OnValidate()
    {
        CacheComponents();

        if (!Application.isPlaying)
        {
            QueueEditorSleepingState();
        }
    }

    public override void OnEchoPulse(Vector3 origin, float radius, float duration)
    {
        if (awakeRoutine != null)
        {
            StopCoroutine(awakeRoutine);
        }

        awakeRoutine = StartCoroutine(AwakeRoutine(awakeDuration));
    }

    private void Update()
    {
        if (!isAwake || triggerCollider == null || Time.time < nextAllowedResetTime)
        {
            return;
        }

        FirstPersonWalkController[] players = FindObjectsByType<FirstPersonWalkController>();
        for (int i = 0; i < players.Length; i++)
        {
            CharacterController controller = players[i].GetComponent<CharacterController>();
            if (controller != null && triggerCollider.bounds.Intersects(controller.bounds))
            {
                ResetPlayer(players[i]);
                return;
            }
        }
    }

    private IEnumerator AwakeRoutine(float duration)
    {
        SetAwakeState(true);
        yield return new WaitForSeconds(duration);
        SetAwakeState(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryResetFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryResetFromCollider(other);
    }

    private void TryResetFromCollider(Collider other)
    {
        if (!isAwake)
        {
            return;
        }

        FirstPersonWalkController player = other.GetComponentInParent<FirstPersonWalkController>();
        if (player != null)
        {
            ResetPlayer(player);
        }
    }

    private void ResetPlayer(FirstPersonWalkController player)
    {
        nextAllowedResetTime = Time.time + resetCooldown;
        player.ResetToSpawn();
    }

    private void SetAwakeState(bool awake)
    {
        isAwake = awake;
        Material material = awake ? awakeMaterial : sleepingMaterial;

        bool rendererEnabled = awake || !hideWhenSleeping;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = rendererEnabled;
        }

        if (material == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
        }
    }

    private void CacheComponents()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        if (triggerCollider == null)
        {
            triggerCollider = GetComponentInChildren<Collider>(true);
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void QueueEditorSleepingState()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += ApplyEditorSleepingState;
#endif
    }

#if UNITY_EDITOR
    private void ApplyEditorSleepingState()
    {
        if (this == null || Application.isPlaying)
        {
            return;
        }

        CacheComponents();
        SetAwakeState(false);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
