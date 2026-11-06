using UnityEngine;

[DisallowMultipleComponent]
public class InspectableItem : MonoBehaviour
{
    public string displayName = "Item";
    [TextArea] public string description;
}
