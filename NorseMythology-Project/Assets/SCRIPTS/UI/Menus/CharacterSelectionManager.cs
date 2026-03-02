using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class ClassSelectionManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private GameObject classGridItemPrefab;
    [SerializeField] private Transform gridParent;

    [Header("UI Panel References")]
    [SerializeField] private CharacterDetailsUI detailsPanel;
    [SerializeField] private GameObject startGameButton;

    [Header("Selection State")]
    public CharacterClass selectedClass;
    private List<ClassGridItem> spawnedItems = new List<ClassGridItem>();

    void Start()
    {
        if (startGameButton) startGameButton.SetActive(false);
        if (detailsPanel) detailsPanel.ClearUI();
        
        LoadAndPopulateGrid();
    }

    private void LoadAndPopulateGrid()
    {
        // 1. Load all classes from Resources/Classes
        CharacterClass[] loadedClasses = Resources.LoadAll<CharacterClass>("Classes");
        
        // 2. Clear existing grid
        foreach (Transform child in gridParent) Destroy(child.gameObject);
        spawnedItems.Clear();

        // 3. Spawn items
        foreach (CharacterClass c in loadedClasses)
        {
            GameObject obj = Instantiate(classGridItemPrefab, gridParent);
            ClassGridItem item = obj.GetComponent<ClassGridItem>();
            
            // We pass 'this' so the item knows who to talk to
            item.Setup(c, this); 
            spawnedItems.Add(item);
        }
    }

    public void SelectClass(CharacterClass newClass)
    {
        selectedClass = newClass;
        
        // Show selected info (No comparison yet)
        detailsPanel.UpdateUI(selectedClass, null);
        
        if (startGameButton) startGameButton.SetActive(true);

        // Update grid highlights
        foreach (var item in spawnedItems)
        {
            item.UpdateVisuals(item.myClass == selectedClass);
        }
    }

    public void OnHoverEnter(CharacterClass hoveredClass)
    {
        // If we have a selection, compare hovered against selected
        // If no selection, just show hovered stats plainly
        detailsPanel.UpdateUI(hoveredClass, selectedClass);
    }

    public void OnHoverExit()
    {
        // If we move mouse away, show the selected class again (or clear if none)
        if (selectedClass != null)
            detailsPanel.UpdateUI(selectedClass, null);
        else
            detailsPanel.ClearUI();
    }

    public void StartGame()
    {
        if (selectedClass == null) return;

        // Pass selection to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.SetSelectedClass(selectedClass);

        SceneManager.LoadScene("GameScene");
    }
}