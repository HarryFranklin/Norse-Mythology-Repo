using UnityEngine;
using UnityEngine.UI;

public class AbilitySlotUI : MonoBehaviour
{
    public Image abilityIcon;
    public Image cooldownOverlay;

    private AbilityManager abilityManager;
    private int abilityIndex;

    public void Initialise(AbilityManager manager, int index)
    {
        abilityManager = manager;
        abilityIndex = index;

        Ability ability = manager.equippedAbilities[index];
        if (ability != null)
        {
            abilityIcon.sprite = ability.abilityIcon;
            abilityIcon.enabled = true;
        }
        else
        {
            abilityIcon.enabled = false;
        }
    }

    void Update()
    {
        if (abilityManager == null) return;

        Ability ability = abilityManager.equippedAbilities[abilityIndex];
        if (ability == null) return;

        float remaining = abilityManager.GetAbilityCooldownRemaining(abilityIndex);
        float max = ability.cooldown * (1f - (abilityManager.playerController.currentStats.abilityCooldownReduction / 100f));

        // Need to invert this so the bar goes the other way
        cooldownOverlay.fillAmount = Mathf.Clamp01(remaining / max);
    }
}