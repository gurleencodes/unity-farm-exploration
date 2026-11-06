using UnityEngine;

// Only objects explicitly marked with this component can be collected.
[DisallowMultipleComponent]
public class StoryPickupItem : MonoBehaviour
{
    public string displayName;
    [TextArea(2, 5)] public string story;
    [HideInInspector] public bool collected;
}
