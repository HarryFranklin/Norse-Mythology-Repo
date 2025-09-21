using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FreezeTimeAbility", menuName = "Abilities/FreezeTime")]
public class FreezeTimeAbility : Ability
{
    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        if (!CanActivate(player))
            return;

        Debug.Log("Not yet implemented.");
    }
}