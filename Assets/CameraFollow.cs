using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 10, -8);

    void Start()
    {
        // Find the player automatically
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }
    
    void LateUpdate()
    {
        if (player == null) return;
        
        // Position camera above and behind player instantly (no smooth movement)
        transform.position = player.position + offset;

        // Look at player
        transform.LookAt(player);
    }
}
