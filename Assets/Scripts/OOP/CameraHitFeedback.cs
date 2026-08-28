using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(2000)]
public sealed class CameraHitFeedback : MonoBehaviour
{
    private Transform m_FollowTarget;
    private Vector3 m_FollowOffset;
    private Quaternion m_LockedRotation;
    private bool m_LockRequested;
    private float m_Amplitude;
    private float m_Remaining;
    private float m_Duration;
    private Vector3 m_LastOffset;
    private float m_NoiseSeed;

    private void Awake()
    {
        m_NoiseSeed = Random.value * 1000f;
    }

    public void AddImpulse(float amplitude, float duration)
    {
        m_Amplitude = Mathf.Clamp(Mathf.Max(m_Amplitude, amplitude), 0f, 0.12f);
        m_Remaining = Mathf.Max(m_Remaining, duration);
        m_Duration = Mathf.Max(0.01f, Mathf.Max(m_Duration, duration));
    }

    public void LockTo(Transform player, Vector3 worldOffset, Quaternion rotation)
    {
        m_FollowTarget = player;
        m_FollowOffset = worldOffset;
        m_LockedRotation = rotation;
        m_LockRequested = true;
        enabled = true;
        ApplyLockedPose();
    }

    private void LateUpdate()
    {
        if (m_LockRequested)
        {
            if (m_FollowTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                m_FollowTarget = player != null ? player.transform : null;
            }
            ApplyLockedPose();
        }
        else
        {
            transform.position -= m_LastOffset;
        }
        m_LastOffset = Vector3.zero;
        if (m_Remaining <= 0f) return;

        m_Remaining = Mathf.Max(0f, m_Remaining - Time.unscaledDeltaTime);
        float envelope = m_Remaining / m_Duration;
        float time = Time.unscaledTime * 38f;
        float x = Mathf.PerlinNoise(m_NoiseSeed, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(m_NoiseSeed + 17.3f, time) * 2f - 1f;
        m_LastOffset = (transform.right * x + transform.up * y) * m_Amplitude * envelope;
        transform.position += m_LastOffset;

        if (m_Remaining <= 0f)
        {
            m_Amplitude = 0f;
            m_Duration = 0f;
        }
    }

    private void OnDisable()
    {
        if (m_LockRequested)
            ApplyLockedPose();
        else
            transform.position -= m_LastOffset;
        m_LastOffset = Vector3.zero;
        m_Remaining = 0f;
        m_Amplitude = 0f;
    }

    private void ApplyLockedPose()
    {
        if (m_FollowTarget == null) return;
        transform.SetPositionAndRotation(
            m_FollowTarget.position + m_FollowOffset,
            m_LockedRotation);
    }
}
