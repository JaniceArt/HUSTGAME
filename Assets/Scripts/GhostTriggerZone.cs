using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GhostTriggerZone : MonoBehaviour
{
    private void Awake()
    {
        // Tự động ép thành Trigger để người chơi không bị đập mặt vào tường vô hình
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem người chạm vào có phải là Player không (Check qua tag hoặc component)
        if (other.CompareTag("Player") || other.GetComponentInChildren<Camera>() != null)
        {
            LaughingGhostEventStep ghostEvent = Object.FindObjectOfType<LaughingGhostEventStep>();
            if (ghostEvent != null)
            {
                ghostEvent.TriggerGhostRunAway();
            }
        }
    }
}
