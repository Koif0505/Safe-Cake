using UnityEngine;

public class VRMovement : MonoBehaviour
{
    void Awake()
    {
        Debug.LogWarning("VRMovement is deprecated. Disable or remove this component to avoid double movement with CharacterControlHybrid.");
        enabled = false;
    }
}
