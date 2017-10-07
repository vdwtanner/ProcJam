﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// This class exists to take some work from the AssetManagerWindow script so that it looks cleaner
/// </summary>
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
	public void FindAllPrefabsInDirectory(string dir)
	{
		if (currentPath != dir)
		{
			allPrefabs = Directory.GetFiles(dir, "*.prefab", SearchOption.AllDirectories);
			currentPath = dir;
		}
	}

	public int HowManyAssetsLeftToAdd()
	{
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
		if (!assetPath.Contains(currentPath.Substring(Application.dataPath.Length)))
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
	/// <returns></returns>
	public T CreateAssetDescription<T>(GameObject gameObject) where T : AAssetDesc
	{
		string assetPath = AssetDatabase.GetAssetPath(gameObject);
		var type = typeof(T);
		if (currentDesc != null && currentDesc.GetType() == type)
		{
			if (currentDesc.path == assetPath)
				return currentDesc as T;
		}
		var ctors = type.GetConstructors();
		currentDesc = ctors[0].Invoke(new object[] { }) as AAssetDesc;
		currentDesc.path = assetPath;
		int start = assetPath.LastIndexOf('/') + 1;
		int length = assetPath.LastIndexOf('.') - start;
		currentDesc.name = assetPath.Substring(start, length);// ".../name.prefab" Want to only get "name"
		return currentDesc as T;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="assetManager"></param>
	/// <returns>Empty string if everything is good, else an error message</returns>
	public string MoveAssetIntoAssetManager(AAssetManager assetManager)
	{
		string newPath = Application.dataPath + "/Resources" + currentDesc.path.Substring(currentDesc.path.LastIndexOf('/'));
		string result = AssetDatabase.MoveAsset(currentDesc.path, newPath);
		if (result == newPath)
		{
			currentDesc.path = newPath;
			assetManager.AddAssetAsync(currentDesc);
			return "";
		}
		return result;
	}
}