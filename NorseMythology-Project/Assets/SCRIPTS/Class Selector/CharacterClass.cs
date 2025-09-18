using UnityEngine;

[CreateAssetMenu(fileName = "New Character Class", menuName = "Game/Character Class")]
public class CharacterClass : ScriptableObject
{
    [Header("Class Info")]
    public string className;
    public Sprite classSprite;
    public string description;

    [Header("Starting Stats")]
    public PlayerStats startingStats; 
}