using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class AssetManagerWindow : EditorWindow {
	const string PROP_FOLDER_KEY = "PropFolder";
	const string WEAPON_FOLDER_KEY = "WeaponFolder";

	private AssetManagerWindowHelper helper;
	private GameObject currentObject;

	private List<System.Type> descriptorTypes;

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
		window.titleContent.text = "Asset Manager";
	}

	void Awake()
	{
		ConditionalInit();
		titleContent.text = "Asset Manager";
	}

	//Init something if null
	void ConditionalInit(bool force = false)
	{
		if (tabs == null || force)
		{
			var descriptions = typeof(AAssetDesc).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(AAssetDesc)));
			List<string> tabNames = new List<string>();
			descriptorTypes = new List<System.Type>();
			foreach(System.Type descriptionType in descriptions)
			{
				string className = descriptionType.Name;
				if (className.Substring(className.Length - 4) == "Desc")
				{
					className = className.Substring(0, className.Length - 4);
				}
				if(className.Substring(className.Length - 5) == "Asset")
				{
					className = className.Substring(0, className.Length - 5);
				}
				className += "s";
				tabNames.Add(className);
				descriptorTypes.Add(descriptionType);
			}
			tabNames.Add("Settings");
			tabs = tabNames.ToArray();
		}
			

		if (helper == null || force)
			helper = new AssetManagerWindowHelper();
	}

	void OnSelectionChange()
	{
		helper.FindAllPrefabsInDirectory(helper.currentPath, true);
		Repaint();
	}

	void OnProjectChange()
	{
		helper.FindAllPrefabsInDirectory(helper.currentPath, true);
		ConditionalInit(true);
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
		if(tab == tabs.Length - 1)
		{
			DrawSettingsTab();
		}
		else
		{
			DrawAssetTab();
		}
	}

	#region AssetTab

	void DrawAssetTab()
	{
		if (descriptorTypes == null)
		{
			ConditionalInit(true);
		}
		string folder = "";
		string folderKey = folder + "_FolderKey";
		if (EditorPrefs.HasKey(folderKey))
		{
			folder = EditorPrefs.GetString(folderKey);
			helper.FindAllPrefabsInDirectory(folder);

			if (mode == 0)
				DrawAssetTabAdd();
			else
				DrawAssetTabUpdate();
		}
		

		DrawSetFolderButton(folderKey);
	}

	void DrawAssetTabAdd()
	{
		EditorGUILayout.LabelField("Remaining Assets: " + helper.HowManyAssetsLeftToAdd(), EditorStyles.boldLabel);
		EditorGUILayout.Space();

		int errorCode = helper.GetSelectedObject(ref currentObject);
		if (errorCode != AssetManagerWindowHelper.NO_ERROR)
		{
			HandleErrorCodes(errorCode);
			//TODO ask if you want to stop editing this object
			if (currentObject == null)
				return;
			else
				EditorGUILayout.HelpBox("Last valid selection shown below.", MessageType.Info);
		}
		AAssetDesc desc = helper.CreateAssetDescription(currentObject, descriptorTypes[tab]);

		

		#region Draw Asset Desc
		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(130));
			DrawAssetPreview(currentObject);
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical(GUILayout.MaxWidth(350));
			{
				EditorGUIUtility.labelWidth = 14 * 8;

				//Get all of the values from the descriptor and render them
				var fields = desc.GetType().GetFields();
				foreach (var field in fields)
				{
					if (!field.IsPublic || field.GetCustomAttributes(typeof(HideInInspector), true).Length > 0)
					{
						continue;
					}
					if (field.FieldType == typeof(int))
					{
						field.SetValue(desc, EditorGUILayout.IntField(field.Name, (int)field.GetValue(desc)));
					}
					else if (field.FieldType.IsEnum)
					{
						if(field.FieldType.GetCustomAttributes(typeof(System.FlagsAttribute), true).Length > 0)
						{
							field.SetValue(desc, EditorGUILayout.EnumMaskPopup(field.Name, field.GetValue(desc) as System.Enum));
						}
						else
						{
							field.SetValue(desc, EditorGUILayout.EnumPopup(field.Name, field.GetValue(desc) as System.Enum));
						}
					}
					else if (field.FieldType == typeof(bool))
					{
						field.SetValue(desc, EditorGUILayout.Toggle(field.Name, (bool)field.GetValue(desc)));
					}
					else if (field.FieldType == typeof(float))
					{
						field.SetValue(desc, EditorGUILayout.FloatField(field.Name, (float)field.GetValue(desc)));
					}
					else if (field.FieldType == typeof(string))
					{
						object[] attributes = field.GetCustomAttributes(typeof(VarcharAttribute), true);
						if (attributes.Length > 0 && false)		//This isn't working how I'd expected
						{
							VarcharAttribute varchar = (VarcharAttribute)attributes[0];
							string val = EditorGUILayout.TextField(field.Name, field.GetValue(desc).ToString());
							
							if (val.Length > varchar.length)
							{
								val = val.Substring(0, varchar.length);
								
							}
							field.SetValue(desc, val);
						}
						else
						{
							field.SetValue(desc, EditorGUILayout.TextField(field.Name, field.GetValue(desc).ToString()));
						}
					}
				}
				/*desc.name = EditorGUILayout.TextField("Name", desc.name);
				desc.theme = (AAssetDesc.AssetTheme)EditorGUILayout.EnumMaskPopup("Theme", desc.theme);
				desc.propType = (PropAssetDesc.PropType)EditorGUILayout.EnumPopup("Prop Type", desc.propType);
				desc.size = (AAssetDesc.AssetSize)EditorGUILayout.EnumPopup("Size", desc.size);
				desc.primaryColor = (AAssetDesc.AssetColor)EditorGUILayout.EnumPopup("Primary Color", desc.primaryColor);
				desc.secondaryColor = (AAssetDesc.AssetColor)EditorGUILayout.EnumPopup("Secondary Color", desc.secondaryColor);
				desc.emitsLight = EditorGUILayout.Toggle("Emits Light", desc.emitsLight);
				if (desc.emitsLight)
				{
					desc.lightColor = (AAssetDesc.AssetColor)EditorGUILayout.EnumPopup("Light Color", desc.lightColor);
				}*/
			}
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();
		#endregion

		if (GUILayout.Button("Move into Asset Manager"))
		{
			//TODO - Update to use the Generic Asset Manager
			string result = helper.MoveAssetIntoAssetManager();
			if (result.Length > 0)
			{
				EditorUtility.DisplayDialog("Error moving into Asset Manager", result, "Shit");
			}
			else
			{
				currentObject = null;
				Selection.activeGameObject = null;
				string folder = EditorPrefs.GetString(PROP_FOLDER_KEY);
				helper.FindAllPrefabsInDirectory(folder, true);
			}
		}

	}

	void DrawAssetTabUpdate()
	{

	}

	#endregion

	#region PropsTab

	void DrawPropsTab()
	{
		string folder = "";
		if (EditorPrefs.HasKey(PROP_FOLDER_KEY))
		{
			folder = EditorPrefs.GetString(PROP_FOLDER_KEY);
			helper.FindAllPrefabsInDirectory(folder);

			if (mode == 0)
				DrawPropsTabAdd();
			else
				DrawPropsTabUpdate();
		}

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
			if(currentObject == null)
				return;
			else
				EditorGUILayout.HelpBox("Last valid selection shown below.", MessageType.Info);
		}
		//PropAssetDesc desc = helper.CreateAssetDescription<PropAssetDesc>(currentObject);

		#region Draw Asset Desc
		/*EditorGUILayout.BeginHorizontal();
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
		EditorGUILayout.EndHorizontal();*/
		#endregion

		if (GUILayout.Button("Move into Asset Manager"))
		{
			//TODO - Update to use the Generic Asset Manager
			/*string result = helper.MoveAssetIntoAssetManager(PropAssetManager.Instance);
			if (result.Length > 0)
			{
				EditorUtility.DisplayDialog("Error moving into Asset Manager", result, "Shit");
			}
			else
			{
				currentObject = null;
				Selection.activeGameObject = null;
				string folder = EditorPrefs.GetString(PROP_FOLDER_KEY);
				helper.FindAllPrefabsInDirectory(folder, true);
			}*/
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
			AssetManager.TerminateConnection();
		}
		if(GUILayout.Button("Re-init window"))
		{
			ConditionalInit(true);
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
