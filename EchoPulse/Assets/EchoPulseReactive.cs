using UnityEngine;

public abstract class EchoPulseReactive : MonoBehaviour
{
    public abstract void OnEchoPulse(Vector3 origin, float radius, float duration);
}
