using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetManagerTester : MonoBehaviour {
	public PropAssetDesc propDesc;

	// Use this for initialization
	void Start () {
		StartCoroutine(PropAssetManager.Instance.AddAssetAsync(propDesc, "totesARealPath.asset"));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
