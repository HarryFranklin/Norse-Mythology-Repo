using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities/Ability")]
[System.Serializable]
public class Ability : ScriptableObject
{
    public string abilityName;
    public string description;
    public GameObject prefab;
    public Color abilityColor = Color.white;
    public int abilityLevel = 1;
    public int maxLevel = 5;
    public AbilityRarity rarity = AbilityRarity.Common;
    
    public virtual void Use(Vector3 position)
    {
        if (prefab != null)
        {
            GameObject obj = GameObject.Instantiate(prefab, position, Quaternion.identity);

            // Recolor sprite if a SpriteRenderer exists
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = abilityColor;
            }
        }
    }
    
    public virtual bool CanUpgrade()
    {
        return abilityLevel < maxLevel;
    }
    
    public virtual void Upgrade()
    {
        if (CanUpgrade())
        {
            abilityLevel++;
        }
    }
    
    public virtual Ability CreateUpgraded()
    {
        Ability upgraded = CreateInstance<Ability>();
        upgraded.abilityName = this.abilityName;
        upgraded.description = this.description;
        upgraded.prefab = this.prefab;
        upgraded.abilityColor = this.abilityColor;
        upgraded.abilityLevel = this.abilityLevel + 1;
        upgraded.maxLevel = this.maxLevel;
        upgraded.rarity = this.rarity;
        return upgraded;
    }
}

public enum AbilityRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}