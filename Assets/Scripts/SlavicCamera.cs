using UnityEngine;

public class SlavicCamera : MonoBehaviour
{
    [Header("Settings")]
    public float followSpeed = 2f;
    public float minDistance = 8f;
    public float maxDistance = 20f;
    public float padding = 3f;
    public Vector3 offset = new Vector3(0, 5, -8);

    void LateUpdate()
    {
        PlayerSlavic[] allPlayers = FindObjectsOfType<PlayerSlavic>();
        if (allPlayers.Length == 0) return;

        Vector3 center;
        float targetDistance;

        if (allPlayers.Length == 1)
        {
            center = allPlayers[0].transform.position;
            targetDistance = minDistance;
        }
        else
        {
            Bounds bounds = new Bounds(allPlayers[0].transform.position, Vector3.zero);
            for (int i = 1; i < allPlayers.Length; i++)
            {
                bounds.Encapsulate(allPlayers[i].transform.position);
            }

            center = bounds.center;
            float maxDim = Mathf.Max(bounds.size.x, bounds.size.z);
            targetDistance = Mathf.Clamp(maxDim + padding, minDistance, maxDistance);
        }

        float scale = targetDistance / minDistance;
        Vector3 scaledOffset = new Vector3(offset.x * scale, offset.y * scale, offset.z * scale);

        Vector3 targetPos = center + scaledOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        transform.LookAt(center);
    }
}
