using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// This class exists to take some work from the AssetManagerWindow script so that it looks cleaner
/// </summary>
[ExecuteInEditMode]
public class AssetManagerWindowHelper {
	public string currentPath { get; private set; }
	public AAssetDesc currentDesc;
	string[] allPrefabs;

	#region ErrorCodes
	public const int NO_ERROR = 0;

	//0 - 10 are for GetSelectedObject
	public const int NO_SELECTION = 1;
	public const int MULTIPLE_SELECTIONS = 2;
	public const int SELECTION_IN_WRONG_FOLDER = 3;
	public const int SELECTED_FOLDER = 4;
	#endregion

	public AssetManagerWindowHelper()
	{
		currentPath = "";
		
	}

	/// <summary>
	/// Finds all prefabs in the specified directory and stores their locations for later use
	/// </summary>
	/// <param name="dir"></param>
	public void FindAllPrefabsInDirectory(string dir, bool force=false)
	{
		if (!System.Uri.IsWellFormedUriString(dir, System.UriKind.Absolute))
		{
			allPrefabs = new string[] { };
			return;
		}
		if (force || currentPath != dir)
		{
			allPrefabs = Directory.GetFiles(dir, "*.prefab", SearchOption.AllDirectories);
			currentPath = dir;
		}
	}

	public int HowManyAssetsLeftToAdd()
	{
		if(allPrefabs == null)
		{
			FindAllPrefabsInDirectory(currentPath, true);
		}
		return allPrefabs.Length;
	}

	/// <summary>
	/// Returns 0 if a valid gameObject was Selected
	/// Returns -1 if multiple objects were selected
	/// Returns 
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public int GetSelectedObject(ref GameObject gameObject)
	{
		string[] guids = Selection.assetGUIDs;
		if(guids.Length == 0)
		{
			return NO_SELECTION;
		}
		if(guids.Length > 1)
		{
			return MULTIPLE_SELECTIONS;
		}
		string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
		//Debug.Log(assetPath + " : " + currentPath.Substring(Application.dataPath.Length - 6));
		if (!assetPath.Contains(currentPath.Substring(Application.dataPath.Length - 6)))
		{
			return SELECTION_IN_WRONG_FOLDER;
		}
		if(Directory.Exists(Application.dataPath + assetPath.Substring(6))) //"Assets" is 6 characters long and we don't want to repeat it
		{
			return SELECTED_FOLDER;
		}
		gameObject = Selection.activeGameObject;

		return NO_ERROR;
	}

	/// <summary>
	/// Create a description if needed
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="gameObject"></param>
	/// <param name="descriptorType">The type of descriptor we want to create</param>
	/// <returns>An AAssetDescriptor that can be cast to the type specified by descriptor type</returns>
	public AAssetDesc CreateAssetDescription(GameObject gameObject, System.Type descriptorType)
	{
		string assetPath = AssetDatabase.GetAssetPath(gameObject);
		if (currentDesc != null && currentDesc.GetType() == descriptorType)
		{
			if (currentDesc.path == assetPath)
				return currentDesc as AAssetDesc;
		}
		var ctors = descriptorType.GetConstructors();
		currentDesc = ctors[0].Invoke(new object[] { }) as AAssetDesc;
		currentDesc.path = assetPath;
		int start = assetPath.LastIndexOf('/') + 1;
		int length = assetPath.LastIndexOf('.') - start;
		currentDesc.name = assetPath.Substring(start, length);// ".../name.prefab" Want to only get "name"
		return currentDesc as AAssetDesc;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="assetManager"></param>
	/// <returns>Empty string if everything is good, else an error message</returns>
	public string MoveAssetIntoAssetManager()
	{
		int assetsEnd = currentDesc.path.IndexOf('/') + 1;
		string newPath = "Assets/Resources/" + currentDesc.path.Substring(assetsEnd);
		string result = AssetDatabase.MoveAsset(currentDesc.path, newPath);
		if (result.Length == 0)
		{
			currentDesc.path = newPath;

			AssetManager.Instance.AddAssetAsync(currentDesc);
			//currentDesc = null;
			FindAllPrefabsInDirectory(currentPath);
			return "";
		}
		return result;
	}
}