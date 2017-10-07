using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AAssetDesc), true)]
public class AssetDescInspector : Editor {

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
	}
}
