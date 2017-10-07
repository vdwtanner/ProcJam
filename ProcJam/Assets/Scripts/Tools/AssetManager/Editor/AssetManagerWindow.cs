using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetManagerWindow : EditorWindow {
	const string PROP_FOLDER_KEY = "PropFolder";
	const string WEAPON_FOLDER_KEY = "WeaponFolder";

	private AssetManagerWindowHelper helper;
	private GameObject currentObject;


	/// <summary>
	/// The current tab.
	/// 0 - Props
	/// </summary>
	private int tab;

	/// <summary>
	/// Mode the asset manager window is in
	/// 0 - Add
	/// 1 - Update
	/// </summary>
	private int mode;

	private string[] tabs;


	[MenuItem("Window/Asset Manager")]
	static void OpenWindow()
	{
		AssetManagerWindow window = EditorWindow.GetWindow<AssetManagerWindow>();
		//Window setup
		window.tab = 0;
		window.minSize = new Vector2(370, 500);
		window.Show();
	}

	//Init something if null
	void ConditionalInit()
	{
		if (tabs == null)
			tabs = new string[] { "Props", "Weapons", "Settings" };

		if (helper == null)
			helper = new AssetManagerWindowHelper();
	}

	void OnSelectionChange()
	{
		Repaint();
	}

	void OnGUI()
	{
		ConditionalInit();
		tab = GUILayout.Toolbar(tab, tabs);
		if(tab < tabs.Length - 1)
		{
			//General buttons and such
			mode = GUILayout.Toolbar(mode, new string[] { "Add", "View/Update" });
		}
		
		EditorGUILayout.Space();
		switch (tab)
		{
			case 0:
				DrawPropsTab();
				break;
			case 1:
				DrawWeaponsTab();
				break;
			default:
				DrawSettingsTab();
				break;
		}
	}

	#region PropsTab

	void DrawPropsTab()
	{
		string folder = "";
		if (EditorPrefs.HasKey(PROP_FOLDER_KEY))
		{
			folder = EditorPrefs.GetString(PROP_FOLDER_KEY);
			helper.FindAllPrefabsInDirectory(folder);
		}


		if (mode == 0)
			DrawPropsTabAdd();
		else
			DrawPropsTabUpdate();

		EditorGUILayout.Space();
		DrawSetFolderButton(PROP_FOLDER_KEY);
	}

	

	void DrawPropsTabAdd()
	{
		//
		EditorGUILayout.LabelField("Remaining Assets: " + helper.HowManyAssetsLeftToAdd(), EditorStyles.boldLabel);
		EditorGUILayout.Space();

		int errorCode = helper.GetSelectedObject(ref currentObject);
		if(errorCode != AssetManagerWindowHelper.NO_ERROR)
		{
			HandleErrorCodes(errorCode);
			//TODO ask if you want to stop editing this object
			if(!currentObject)
				return;
			else
				EditorGUILayout.HelpBox("Last valid selection shown below.", MessageType.Info);
		}
		PropAssetDesc desc = helper.CreateAssetDescription<PropAssetDesc>(currentObject);

		
		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(130));
				DrawAssetPreview(currentObject);
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(GUILayout.MaxWidth(350));
			{
				EditorGUIUtility.labelWidth = 14 * 8;
				desc.name = EditorGUILayout.TextField("Name", desc.name);
				desc.theme = (AAssetDesc.AssetTheme)EditorGUILayout.EnumMaskPopup("Theme", desc.theme);
				desc.propType = (PropAssetDesc.PropType)EditorGUILayout.EnumPopup("Prop Type", desc.propType);
				desc.size = (AAssetDesc.AssetSize)EditorGUILayout.EnumPopup("Size", desc.size);
				desc.primaryColor = (AAssetDesc.AssetColor)EditorGUILayout.EnumPopup("Primary Color", desc.primaryColor);
				desc.secondaryColor = (AAssetDesc.AssetColor)EditorGUILayout.EnumPopup("Secondary Color", desc.secondaryColor);
				desc.emitsLight = EditorGUILayout.Toggle("Emits Light", desc.emitsLight);
				if (desc.emitsLight)
				{
					desc.lightColor = (AAssetDesc.AssetColor)EditorGUILayout.EnumPopup("Light Color", desc.lightColor);
				}
			}
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Move into Asset Manager"))
		{
			string result = helper.MoveAssetIntoAssetManager(PropAssetManager.Instance);
			if (result.Length > 0)
			{
				EditorUtility.DisplayDialog("Error moving into Asset Manager", result, "Shit");
			}
			else
			{
				currentObject = null;
				string folder = EditorPrefs.GetString(PROP_FOLDER_KEY);
				helper.FindAllPrefabsInDirectory(folder);
			}
		}
	}

	void DrawPropsTabUpdate()
	{

	}

	#endregion

	#region WeaponsTab
	void DrawWeaponsTab()
	{
		DrawSetFolderButton(WEAPON_FOLDER_KEY);
	}
	#endregion

	#region SettingsTab
	void DrawSettingsTab()
	{
		if (GUILayout.Button("ClearPrefs"))
		{
			EditorPrefs.DeleteKey(PROP_FOLDER_KEY);
			EditorPrefs.DeleteKey(WEAPON_FOLDER_KEY);
		}
		if(GUILayout.Button("Shutdown DB Connections"))
		{
			PropAssetManager.TerminateConnection();
		}
	}
	#endregion


	#region Utilities

	void DrawSetFolderButton(string key)
	{
		bool hasKey = EditorPrefs.HasKey(key);
		if (hasKey)
		{
			string folder = EditorPrefs.GetString(key);
			GUIContent content = new GUIContent("Update Folder Location", "Current is: " + folder);
			if (GUILayout.Button(content))
			{
				folder = EditorUtility.OpenFolderPanel(key, folder, "");
				if (folder.Length > 0)
				{
					EditorPrefs.SetString(key, folder);
				}
			}
		}
		else
		{
			if (GUILayout.Button("Set Folder Location"))
			{
				string folder = EditorUtility.OpenFolderPanel(key, Application.dataPath, "");
				if(folder.Length > 0)
				{
					EditorPrefs.SetString(key, folder);
				}
				
			}
		}
		
	}

	void HandleErrorCodes(int errorCode)
	{
		switch (errorCode)
		{
			case AssetManagerWindowHelper.NO_ERROR:
				break;
			case AssetManagerWindowHelper.NO_SELECTION:
				EditorGUILayout.HelpBox("Please select a Prefab from the Project window.", MessageType.Info);
				break;
			case AssetManagerWindowHelper.MULTIPLE_SELECTIONS:
				EditorGUILayout.HelpBox("Multiple objects selected", MessageType.Warning);
				break;
			case AssetManagerWindowHelper.SELECTION_IN_WRONG_FOLDER:
				EditorGUILayout.HelpBox("Selection must be from " + helper.currentPath + " or a subdirectory.", MessageType.Warning);
				break;
			case AssetManagerWindowHelper.SELECTED_FOLDER:
				EditorGUILayout.HelpBox("Folder selected", MessageType.Warning);
				break;
		}
	}

	private void DrawAssetPreview(GameObject objectToAdd)
	{
		EditorGUILayout.LabelField("Asset Preview", EditorStyles.boldLabel, GUILayout.Width(128));
		Rect previewImageRect = EditorGUILayout.GetControlRect(true, 128);
		previewImageRect.width = 128;
		EditorGUI.DrawPreviewTexture(previewImageRect, AssetPreview.GetAssetPreview(objectToAdd));
	}
	#endregion

}
