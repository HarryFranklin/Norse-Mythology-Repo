using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    public List<Ability> abilities = new List<Ability>(4);
    public int playerLevel = 1;
    private int selectedAbilityIndex = 0;
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        // Ability selection (1-4 keys)
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedAbilityIndex = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedAbilityIndex = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedAbilityIndex = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedAbilityIndex = 3;
        
        // Use ability on left click
        if (Input.GetMouseButtonDown(0))
        {
            UseSelectedAbility();
        }
        
        // Force end wave with P key - goes to ability selection
        if (Input.GetKeyDown(KeyCode.P))
        {
            EndWave();
        }
    }
    
    void UseSelectedAbility()
    {
        if (selectedAbilityIndex < abilities.Count && abilities[selectedAbilityIndex] != null)
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = playerCamera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                abilities[selectedAbilityIndex].Use(hit.point);
            }
            else
            {
                Vector3 worldPosition = ray.origin + ray.direction * 10f;
                abilities[selectedAbilityIndex].Use(worldPosition);
            }
        }
    }
    
    void EndWave()
    {
        GameManager.Instance.EndCurrentWave();
    }
    
    public void AddAbility(Ability newAbility)
    {
        if (abilities.Count < 4)
        {
            abilities.Add(newAbility);
        }
    }
    
    public void ReplaceAbility(int index, Ability newAbility)
    {
        if (index >= 0 && index < abilities.Count)
        {
            abilities[index] = newAbility;
        }
    }
}