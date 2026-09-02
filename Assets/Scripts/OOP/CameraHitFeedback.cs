using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(2000)]
public sealed class CameraHitFeedback : MonoBehaviour
{
    [Header("Shake Tuning")]
    [SerializeField, Min(0.01f)] private float m_MaxTranslation = 0.24f;
    [SerializeField, Min(0f)] private float m_MaxRotation = 1.6f;
    [SerializeField, Min(1f)] private float m_DefaultFrequency = 26f;

    private Transform m_FollowTarget;
    private Vector3 m_FollowOffset;
    private Quaternion m_LockedRotation;
    private bool m_LockRequested;
    private float m_Amplitude;
    private float m_Remaining;
    private float m_Duration;
    private Vector3 m_LastOffset;
    private Quaternion m_LastRotationOffset = Quaternion.identity;
    private float m_NoiseSeed;
    private float m_Frequency;
    private float m_RotationAmplitude;

    private void Awake()
    {
        m_NoiseSeed = Random.value * 1000f;
    }

    public void AddImpulse(
        float amplitude,
        float duration,
        float frequency = 0f,
        float rotationAmplitude = 0.5f)
    {
        if (amplitude <= 0f || duration <= 0f) return;

        // Keep repeated automatic-fire impulses alive without allowing them to
        // accumulate into an unreadable camera jump.
        float stackedAmplitude = Mathf.Max(m_Amplitude, amplitude) + amplitude * 0.18f;
        m_Amplitude = Mathf.Clamp(stackedAmplitude, 0f, m_MaxTranslation);
        m_Remaining = Mathf.Max(m_Remaining, duration);
        m_Duration = Mathf.Max(0.01f, Mathf.Max(m_Duration, duration));
        m_Frequency = Mathf.Max(m_Frequency, frequency > 0f ? frequency : m_DefaultFrequency);
        m_RotationAmplitude = Mathf.Clamp(
            Mathf.Max(m_RotationAmplitude, rotationAmplitude),
            0f,
            m_MaxRotation);
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
            transform.rotation *= Quaternion.Inverse(m_LastRotationOffset);
        }
        m_LastOffset = Vector3.zero;
        m_LastRotationOffset = Quaternion.identity;
        if (m_Remaining <= 0f) return;

        m_Remaining = Mathf.Max(0f, m_Remaining - Time.unscaledDeltaTime);
        float normalized = Mathf.Clamp01(m_Remaining / m_Duration);
        float envelope = normalized * normalized * (3f - 2f * normalized);
        float time = Time.unscaledTime * Mathf.Max(1f, m_Frequency);
        float x = Mathf.PerlinNoise(m_NoiseSeed, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(m_NoiseSeed + 17.3f, time) * 2f - 1f;
        float roll = Mathf.PerlinNoise(m_NoiseSeed + 43.7f, time) * 2f - 1f;

        // Vertical movement is intentionally lower so the camera feels like a
        // weapon kick rather than handheld camera jitter.
        m_LastOffset = (transform.right * x + transform.up * y * 0.62f) * m_Amplitude * envelope;
        m_LastRotationOffset = Quaternion.Euler(
            y * m_RotationAmplitude * 0.28f * envelope,
            x * m_RotationAmplitude * 0.22f * envelope,
            roll * m_RotationAmplitude * envelope);
        transform.position += m_LastOffset;
        transform.rotation *= m_LastRotationOffset;

        if (m_Remaining <= 0f)
        {
            m_Amplitude = 0f;
            m_Duration = 0f;
            m_Frequency = 0f;
            m_RotationAmplitude = 0f;
        }
    }

    private void OnDisable()
    {
        if (m_LockRequested)
            ApplyLockedPose();
        else
        {
            transform.position -= m_LastOffset;
            transform.rotation *= Quaternion.Inverse(m_LastRotationOffset);
        }
        m_LastOffset = Vector3.zero;
        m_LastRotationOffset = Quaternion.identity;
        m_Remaining = 0f;
        m_Amplitude = 0f;
        m_Duration = 0f;
        m_Frequency = 0f;
        m_RotationAmplitude = 0f;
    }

    private void ApplyLockedPose()
    {
        if (m_FollowTarget == null) return;
        transform.SetPositionAndRotation(
            m_FollowTarget.position + m_FollowOffset,
            m_LockedRotation);
    }
}
