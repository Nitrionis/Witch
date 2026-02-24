using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CheatsMenu : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private Canvas canvas;
	[SerializeField] private GameObject buttonPrefab;
	[SerializeField] private GameObject menuPanel;

	[Header("Settings")]
	[SerializeField] private float indentAmount = 20f;
	[SerializeField] private float padding = 10f;
	[SerializeField] private int baseFontSize = 14;
	[SerializeField] private float fontScaleFactor = 0.8f;

	private List<IRowInfo> rootItems;
	private Dictionary<SectionInfo, GameObject> sectionPanels = new Dictionary<SectionInfo, GameObject>();
	private Dictionary<SectionInfo, bool> sectionStates = new Dictionary<SectionInfo, bool>();

	void Start()
	{
		InitializeMenu();
		CreateTestData();
		GenerateGUI();
	}

	private void InitializeMenu()
	{
		rootItems?.Clear();
		rootItems ??= new List<IRowInfo>();

		// Create menu panel if not assigned
		Image image = menuPanel.GetComponent<Image>();
		image ??= menuPanel.AddComponent<Image>();
		image.color = new Color(0, 0, 0, 0.8f);
	}

	private void CreateTestData()
	{
		// Create test cheat menu structure
		rootItems.Add(new RowInfo(rowHeight: 80, new List<ButtonInfo>() {
			new ButtonInfo("Close", () => Debug.Log("Health added"), 1f),
		}));

		rootItems.Add(new RowInfo(rowHeight: 60, new List<ButtonInfo>() {
			new ButtonInfo("Add Health", () => Debug.Log("Health added"), 1f),
			new ButtonInfo("Add Money", () => Debug.Log("Money added"), 1f),
			new ButtonInfo("God Mode", () => Debug.Log("God mode toggled"), 1f),
		}));

		rootItems.Add(new RowInfo(rowHeight: 60, new List<ButtonInfo>() {
			new ButtonInfo("Teleport to Start", () => Debug.Log("Teleported to start"), 2f),
			new ButtonInfo("Skip Level", () => Debug.Log("Level skipped"), 1f),
		}));

		// Create a section with nested items
		var weaponsSection = new SectionInfo("Weapons", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Unlock All Weapons", () => Debug.Log("All weapons unlocked"), 1f),
				new ButtonInfo(string.Concat(Enumerable.Repeat("Ammo maxed ", 10)), () => Debug.Log("Ammo maxed"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Infinite Ammo", () => Debug.Log("Infinite ammo toggled"), 1f),
			})
		});

		rootItems.Add(new RowInfo(rowHeight: 60, new List<ButtonInfo> {
			new SectionButtonInfo("Weapons", weaponsSection)
		}));

		// Create nested section
		var playerSection = new SectionInfo("Player", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Max Health", () => Debug.Log("Health maxed"), 1f),
				new ButtonInfo("Max Speed", () => Debug.Log("Speed maxed"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Fly Mode", () => Debug.Log("Fly mode toggled"), 1f),
			})
		});

		rootItems.Add(new RowInfo(rowHeight: 60, new List<ButtonInfo> {
			new SectionButtonInfo("Player", playerSection)
		}));

		// Create a nested section example - Advanced section with sub-sections
		var advancedWeaponsSection = new SectionInfo("Advanced Weapons", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Rocket Launcher", () => Debug.Log("Rocket launcher unlocked"), 1f),
				new ButtonInfo("Sniper Rifle", () => Debug.Log("Sniper rifle unlocked"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Laser Gun", () => Debug.Log("Laser gun unlocked"), 1f),
			})
		});

		// Create a sub-section within the advanced weapons
		var specialAmmoSection = new SectionInfo("Special Ammo", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Explosive Rounds", () => Debug.Log("Explosive rounds added"), 1f),
				new ButtonInfo("Armor Piercing", () => Debug.Log("Armor piercing rounds added"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Incendiary Rounds", () => Debug.Log("Incendiary rounds added"), 1f),
			})
		});

		// Add the special ammo section to the advanced weapons section
		advancedWeaponsSection = new SectionInfo("Advanced Weapons", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Rocket Launcher", () => Debug.Log("Rocket launcher unlocked"), 1f),
				new ButtonInfo("Sniper Rifle", () => Debug.Log("Sniper rifle unlocked"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Laser Gun", () => Debug.Log("Laser gun unlocked"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new SectionButtonInfo("Special Ammo", specialAmmoSection)
			})
		});

		rootItems.Add(new RowInfo(rowHeight: 60, new List<ButtonInfo> {
			new SectionButtonInfo("Advanced Weapons", advancedWeaponsSection)
		}));

		// Create another nested example - Game Settings with sub-categories
		var graphicsSection = new SectionInfo("Graphics", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Ultra Graphics", () => Debug.Log("Graphics set to ultra"), 1f),
				new ButtonInfo("Disable Shadows", () => Debug.Log("Shadows disabled"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("60 FPS Lock", () => Debug.Log("FPS locked to 60"), 1f),
			})
		});

		var audioSection = new SectionInfo("Audio", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Mute All", () => Debug.Log("All audio muted"), 1f),
				new ButtonInfo("Max Volume", () => Debug.Log("Volume set to max"), 1f),
			})
		});

		var gameSettingsSection = new SectionInfo("Game Settings", 60, new List<RowInfo> {
			new RowInfo(60, new List<ButtonInfo> {
				new ButtonInfo("Unlock All Levels", () => Debug.Log("All levels unlocked"), 1f),
				new ButtonInfo("Reset Progress", () => Debug.Log("Progress reset"), 1f),
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new SectionButtonInfo("Graphics", graphicsSection)
			}),
			new RowInfo(60, new List<ButtonInfo> {
				new SectionButtonInfo("Audio", audioSection)
			})
		});

		rootItems.Add(new RowInfo(rowHeight: 60, new List<ButtonInfo> {
			new SectionButtonInfo("Game Settings", gameSettingsSection)
		}));
	}

	private void GenerateGUI()
	{
		// Stop all running coroutines to prevent animation errors
		StopAllCoroutines();

		// Clear existing UI more reliably
		ClearAllChildren(menuPanel.transform);

		sectionPanels.Clear();
		// Don't clear sectionStates here - we want to preserve toggle states

		// Use simple recursive approach
		CreateRowsFromItems(rootItems, 0, null);
	}

	private void ClearAllChildren(Transform parent)
	{
		if (parent == null) return;

		// Create a list to avoid modifying collection while iterating
		var children = new List<Transform>();
		foreach (Transform child in parent) {
			children.Add(child);
		}

		// Destroy all children
		foreach (var child in children) {
			if (child != null) {
				// Try DestroyImmediate first (works in editor and at runtime)
				DestroyImmediate(child.gameObject);
			}
		}

		// Double-check: if any children remain, force clear them
		if (parent.childCount > 0) {
			Debug.LogWarning($"Failed to clear all children. {parent.childCount} children remain.");
			// Force clear remaining children
			while (parent.childCount > 0) {
				var remainingChild = parent.GetChild(0);
				if (remainingChild != null) {
					DestroyImmediate(remainingChild.gameObject);
				} else {
					break; // Prevent infinite loop
				}
			}
		}
	}

	private void CreateRowsFromItems(List<IRowInfo> items, int indentLevel, SectionInfo parentSection)
	{
		foreach (var item in items) {
			if (item is RowInfo rowInfo) {
				CreateRow(rowInfo, indentLevel, parentSection);

				// Check for section buttons in this row
				foreach (var buttonInfo in rowInfo.Buttons) {
					if (buttonInfo is SectionButtonInfo sectionButton && sectionButton.Section != null) {
						// If section is expanded, create its content
						if (IsSectionExpanded(sectionButton.Section)) {
							CreateRowsFromItems(new List<IRowInfo>(sectionButton.Section.Rows), indentLevel + 1, sectionButton.Section);
						}
					}
				}
			}
		}
	}



	private bool IsSectionExpanded(SectionInfo sectionInfo)
	{
		// Check for null section
		if (sectionInfo == null) {
			return false;
		}

		// Initialize section state if not exists (default to collapsed)
		if (!sectionStates.ContainsKey(sectionInfo)) {
			sectionStates[sectionInfo] = false;
		}

		// Simply check if this section is expanded
		return sectionStates[sectionInfo];
	}

	private void CreateRow(RowInfo rowInfo, int indentLevel, SectionInfo parentSection = null)
	{
		GameObject rowObject = new GameObject("Row");
		rowObject.transform.SetParent(menuPanel.transform, false);

		RectTransform rowRect = rowObject.AddComponent<RectTransform>();
		rowRect.sizeDelta = new Vector2(0, rowInfo.RowHeight);

		// Add left margin for indentation
		LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
		layoutElement.minHeight = rowInfo.RowHeight;
		layoutElement.preferredHeight = rowInfo.RowHeight;

		// Add horizontal layout group for buttons
		HorizontalLayoutGroup horizontalLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
		horizontalLayout.spacing = 5f;
		horizontalLayout.padding = new RectOffset((int)(indentLevel * indentAmount), 5, 5, 5);
		horizontalLayout.childControlWidth = true;
		horizontalLayout.childControlHeight = true;
		horizontalLayout.childForceExpandWidth = true;
		horizontalLayout.childForceExpandHeight = true;

		// Calculate total width ratio
		float totalRatio = 0f;
		foreach (var button in rowInfo.Buttons) {
			totalRatio += button.WidthRatio;
		}

		// Create buttons
		foreach (var buttonInfo in rowInfo.Buttons) {
			if (buttonInfo is SectionButtonInfo sectionButton) {
				CreateSectionButton(sectionButton, rowInfo.RowHeight, totalRatio);
			} else {
				CreateCheatButton(buttonInfo, rowInfo.RowHeight, totalRatio);
			}
		}

	}

	private void CreateCheatButton(ButtonInfo buttonInfo, int rowHeight, float totalRatio)
	{
		GameObject buttonObject = Instantiate(buttonPrefab, menuPanel.transform.GetChild(menuPanel.transform.childCount - 1));

		Button button = buttonObject.GetComponent<Button>();
		Image buttonImage = buttonObject.GetComponent<Image>();
		buttonImage.color = buttonInfo.Name == "Close" ? Color.black : new Color(0.2f, 0.2f, 0.2f, 0.8f);

		GameObject textObject = buttonObject.transform.GetChild(0).gameObject;
		
		var buttonText = textObject.GetComponent<TextMeshProUGUI>();
		buttonText.text = buttonInfo.Name;
		buttonText.color = Color.white;

		button.onClick.AddListener(() => buttonInfo.Clicked?.Invoke());

		// Set flexible width based on ratio
		LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = buttonInfo.WidthRatio / totalRatio;
	}

	private void CreateSectionButton(SectionButtonInfo sectionButton, int rowHeight, float totalRatio)
	{
		GameObject buttonObject = Instantiate(buttonPrefab, menuPanel.transform.GetChild(menuPanel.transform.childCount - 1));

		Button button = buttonObject.GetComponent<Button>();
		Image buttonImage = buttonObject.GetComponent<Image>();
		buttonImage.color = new Color(0.3f, 0.3f, 0.5f, 0.8f);

		GameObject textObject = buttonObject.transform.GetChild(0).gameObject;

		var buttonText = textObject.GetComponent<TextMeshProUGUI>();
		buttonText.text = (IsSectionExpanded(sectionButton.Section) ? " - " : " + ") + sectionButton.Name;
		buttonText.color = Color.white;
		buttonText.horizontalAlignment = HorizontalAlignmentOptions.Left;

		button.onClick.AddListener(() => ToggleSection(sectionButton.Section));

		// Set flexible width based on ratio
		LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = sectionButton.WidthRatio / totalRatio;
	}

	private void ToggleSection(SectionInfo sectionInfo)
	{
		if (sectionInfo == null) {
			return; // Don't process null sections
		}

		if (!sectionStates.ContainsKey(sectionInfo)) {
			sectionStates[sectionInfo] = false;
		}

		sectionStates[sectionInfo] = !sectionStates[sectionInfo];

		// Regenerate GUI to show/hide section content
		GenerateGUI();
	}

	public class SectionInfo : IRowInfo
	{
		public readonly string Name;
		public readonly IReadOnlyCollection<IRowInfo> Rows;

		public int RowHeight { get; }

		public SectionInfo(string name, int rowHeight, IReadOnlyCollection<RowInfo> rows)
		{
			Name = name;
			Rows = rows;
			RowHeight = rowHeight;
		}
	}

	public class RowInfo : IRowInfo
	{
		public readonly IReadOnlyCollection<ButtonInfo> Buttons;

		public int RowHeight { get; }

		public RowInfo(int rowHeight, List<ButtonInfo> buttons)
		{
			RowHeight = rowHeight;
			Buttons = buttons;
		}
	}

	public interface IRowInfo
	{
		int RowHeight { get; }
	}

	public class ButtonInfo
	{
		public readonly string Name;
		public readonly Action Clicked;
		public readonly float WidthRatio = 1f;

		public ButtonInfo(string name, Action clicked, float widthRatio = 1)
		{
			Name = name;
			Clicked = clicked;
			WidthRatio = widthRatio;
		}
	}

	public class SectionButtonInfo : ButtonInfo
	{
		public readonly SectionInfo Section;

		public SectionButtonInfo(string name, SectionInfo section, float widthRatio = 1)
			: base(name, null, widthRatio)
		{
			Section = section;
		}
	}
}