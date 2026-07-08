using UnityEngine;
using Mirror;

public class PickupItem : NetworkBehaviour
{
    public bool isHealItem;
    public int bonusDamage = 30;
    public int healAmount = 30;
    public int weaponVisualIndex;
}
