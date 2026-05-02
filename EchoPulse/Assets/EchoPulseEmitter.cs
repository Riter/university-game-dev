using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

public class EchoPulseEmitter : MonoBehaviour
{
    [Header("Pulse")]
    public Transform pulseOrigin;
    public float radius = 12f;
    public float revealDuration = 3f;
    public float cooldown = 1.5f;
    public LayerMask reactiveLayers = ~0;

    [Header("Visuals")]
    public LineRenderer fallbackRing;
    public ParticleSystem fallbackPulse;
    public VisualEffect visualEffect;
    public Light pulseLight;
    public int ringSegments = 96;
    public float ringDuration = 0.75f;
    public float ringStartRadius = 0.35f;
    public Color ringColor = new Color(0f, 0.85f, 1f, 0.55f);
    public float pulseLightIntensity = 1.2f;
    public float pulseLightDuration = 0.12f;

    [Header("Camera And Post")]
    public CinemachineCamera cinemachineCamera;
    public float fieldOfViewKick = 3f;
    public float kickDuration = 0.25f;
    public Volume globalVolume;
    public float bloomKick = 0.45f;

    private readonly HashSet<EchoPulseReactive> pulseTargets = new HashSet<EchoPulseReactive>();
    private float nextAllowedPulseTime;
    private float baseFieldOfView;
    private Bloom bloom;
    private float baseBloomIntensity;
    private Coroutine fovRoutine;
    private Coroutine bloomRoutine;
    private Coroutine lightRoutine;
    private Coroutine ringRoutine;

    private void Awake()
    {
        if (pulseOrigin == null)
        {
            pulseOrigin = transform;
        }

        if (cinemachineCamera != null)
        {
            baseFieldOfView = cinemachineCamera.Lens.FieldOfView;
        }

        if (globalVolume != null && globalVolume.profile != null && globalVolume.profile.TryGet(out bloom))
        {
            baseBloomIntensity = bloom.intensity.value;
        }

        if (pulseLight != null)
        {
            pulseLight.enabled = false;
        }
    }

    private void Update()
    {
        if (WantsPulse() && Time.time >= nextAllowedPulseTime)
        {
            FirePulse();
        }
    }

    private bool WantsPulse()
    {
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool enterPressed = Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);

        return mousePressed || enterPressed;
    }

    private void FirePulse()
    {
        nextAllowedPulseTime = Time.time + cooldown;

        Vector3 origin = pulseOrigin.position;
        PlayVisuals(origin);
        NotifyReactiveObjects(origin);
        StartCameraKick();
    }

    private void PlayVisuals(Vector3 origin)
    {
        if (fallbackRing != null)
        {
            if (ringRoutine != null)
            {
                StopCoroutine(ringRoutine);
            }

            ringRoutine = StartCoroutine(RingPulseRoutine(origin));
        }
        else if (fallbackPulse != null)
        {
            fallbackPulse.transform.position = origin;
            fallbackPulse.Clear(true);
            fallbackPulse.Play(true);
        }

        if (visualEffect != null && visualEffect.visualEffectAsset != null)
        {
            if (visualEffect.HasVector3("PulseOrigin"))
            {
                visualEffect.SetVector3("PulseOrigin", origin);
            }

            if (visualEffect.HasFloat("PulseRadius"))
            {
                visualEffect.SetFloat("PulseRadius", radius);
            }

            if (visualEffect.HasFloat("PulseDuration"))
            {
                visualEffect.SetFloat("PulseDuration", revealDuration);
            }

            visualEffect.Reinit();
            visualEffect.SendEvent("OnPlay");
        }

        if (pulseLight != null)
        {
            if (lightRoutine != null)
            {
                StopCoroutine(lightRoutine);
            }

            lightRoutine = StartCoroutine(PulseLightRoutine());
        }
    }

    private void NotifyReactiveObjects(Vector3 origin)
    {
        pulseTargets.Clear();

        Collider[] hits = Physics.OverlapSphere(origin, radius, reactiveLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            EchoPulseReactive[] targets = hits[i].GetComponentsInParent<EchoPulseReactive>();
            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                pulseTargets.Add(targets[targetIndex]);
            }
        }

        foreach (EchoPulseReactive target in pulseTargets)
        {
            target.OnEchoPulse(origin, radius, revealDuration);
        }
    }

    private void StartCameraKick()
    {
        if (cinemachineCamera != null)
        {
            if (fovRoutine != null)
            {
                StopCoroutine(fovRoutine);
                cinemachineCamera.Lens.FieldOfView = baseFieldOfView;
            }

            fovRoutine = StartCoroutine(FieldOfViewKickRoutine());
        }

        if (bloom != null)
        {
            if (bloomRoutine != null)
            {
                StopCoroutine(bloomRoutine);
                bloom.intensity.value = baseBloomIntensity;
            }

            bloomRoutine = StartCoroutine(BloomKickRoutine());
        }
    }

    private IEnumerator FieldOfViewKickRoutine()
    {
        cinemachineCamera.Lens.FieldOfView = baseFieldOfView + fieldOfViewKick;
        yield return new WaitForSeconds(kickDuration);
        cinemachineCamera.Lens.FieldOfView = baseFieldOfView;
    }

    private IEnumerator BloomKickRoutine()
    {
        bloom.intensity.value = baseBloomIntensity + bloomKick;
        yield return new WaitForSeconds(kickDuration);
        bloom.intensity.value = baseBloomIntensity;
    }

    private IEnumerator PulseLightRoutine()
    {
        pulseLight.enabled = true;
        pulseLight.intensity = pulseLightIntensity;
        yield return new WaitForSeconds(pulseLightDuration);
        pulseLight.intensity = 0f;
        pulseLight.enabled = false;
    }

    private IEnumerator RingPulseRoutine(Vector3 origin)
    {
        fallbackRing.enabled = true;
        fallbackRing.loop = true;
        fallbackRing.useWorldSpace = true;
        fallbackRing.positionCount = Mathf.Max(8, ringSegments);

        float elapsed = 0f;
        while (elapsed < ringDuration)
        {
            float t = elapsed / ringDuration;
            float eased = 1f - (1f - t) * (1f - t);
            float currentRadius = Mathf.Lerp(ringStartRadius, radius, eased);
            Color color = ringColor;
            color.a *= 1f - t;

            fallbackRing.startColor = color;
            fallbackRing.endColor = color;
            fallbackRing.startWidth = Mathf.Lerp(0.08f, 0.015f, t);
            fallbackRing.endWidth = fallbackRing.startWidth;
            WriteRingPositions(origin, currentRadius);

            elapsed += Time.deltaTime;
            yield return null;
        }

        fallbackRing.enabled = false;
    }

    private void WriteRingPositions(Vector3 origin, float currentRadius)
    {
        int count = fallbackRing.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f * i) / count;
            Vector3 point = origin + new Vector3(Mathf.Cos(angle) * currentRadius, -0.2f, Mathf.Sin(angle) * currentRadius);
            fallbackRing.SetPosition(i, point);
        }
    }

    private void OnDisable()
    {
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens.FieldOfView = baseFieldOfView;
        }

        if (bloom != null)
        {
            bloom.intensity.value = baseBloomIntensity;
        }

        if (pulseLight != null)
        {
            pulseLight.intensity = 0f;
            pulseLight.enabled = false;
        }

        if (fallbackRing != null)
        {
            fallbackRing.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = pulseOrigin != null ? pulseOrigin : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin.position, radius);
    }
}
