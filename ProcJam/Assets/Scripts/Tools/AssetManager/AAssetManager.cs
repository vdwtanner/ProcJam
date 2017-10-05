using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AAssetManager {
	public abstract GameObject GetAsset();
	public abstract void Initialize();
	public abstract void Shutdown();
}
