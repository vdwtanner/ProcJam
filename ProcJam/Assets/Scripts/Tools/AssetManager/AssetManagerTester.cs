using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetManagerTester : MonoBehaviour {
	public PropAssetDesc propDesc;

	// Use this for initialization
	void Start () {
		AssetManager.Instance.AddAssetAsync(propDesc);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
