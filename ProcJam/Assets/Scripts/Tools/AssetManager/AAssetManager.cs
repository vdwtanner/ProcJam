using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public abstract class AAssetManager {
	protected SQLiteConnection sqlCon;
	protected bool tableReady = false;

	protected AAssetManager()
	{
		Initialize();
	}

	#region AssetAdders

	public abstract void AddAssetAsync(AAssetDesc desc);

	protected abstract void AddAsset(object info);

	#endregion

	#region AssetGetters

	/// <summary>
	/// Coroutine that gets an asset asynchronously. Yields until the asset has been retrieved.
	/// </summary>
	/// <param name="desc">Descriptor for the Asset</param>
	/// <param name="gameObject">The GameObject that was retrieved</param>
	/// <returns></returns>
	public abstract IEnumerator GetAssetAsync(AAssetDesc desc, out GameObject gameObject);

	/// <summary>
	/// Coroutine that gets a number of assets that match the descriptor asynchronously. Yields until the assets have been retrieved.
	/// </summary>
	/// <param name="desc">Descriptor for the Assets</param>
	/// <param name="count">How many assets to retrieve</param>
	/// <param name="gameObjects">A List to fill with retrieved Game Objects</param>
	/// <returns></returns>
	public abstract IEnumerator GetAssetsAsync(AAssetDesc desc, int count, out List<GameObject> gameObjects);
#endregion

#region Initialization

	/// <summary>
	/// Initial setup. Will start up a connection to the asset database
	/// </summary>
	public virtual void Initialize()
	{
		sqlCon = new SQLiteConnection();
		sqlCon.ConnectToDatabase("AssetDatabase.db");
		tableReady = false;
		ThreadPool.QueueUserWorkItem(new WaitCallback(ConstructTable));
		Debug.Log("Started thread");
	}

	/// <summary>
	/// Asynchronous construction call to make sure that the table exists
	/// </summary>
	/// <param name="state"></param>
	protected abstract void ConstructTable(object state);
#endregion

	/// <summary>
	/// Run the shutdown sequence. Will disconnect from the asset database
	/// </summary>
	protected virtual void Shutdown()
	{
		sqlCon.Shutdown();
	}
}

[System.Serializable]
public abstract class AAssetDesc {
	/// <summary>
	/// A classification of the size of an asset. Specifics TBD
	/// </summary>
	[System.Flags]
	public enum AssetSize {
		/// <summary>
		/// 
		/// </summary>
		Tiny = 1,
		/// <summary>
		/// 
		/// </summary>
		Small = 2,
		/// <summary>
		/// 
		/// </summary>
		Medium = 4,
		/// <summary>
		/// 
		/// </summary>
		Large = 8,
		/// <summary>
		/// 
		/// </summary>
		Huge = 16,
		Any = Tiny | Small | Medium | Large | Huge
	}
	[System.Flags]
	public enum AssetColor {
		White = 1,
		Grey = 2,
		Black = 4,
		Red = 8,
		Orange = 16,
		Yellow = 32,
		Green = 64,
		Blue = 128,
		Purple = 256,
		Brown = 512,
		Warm = Red | Orange | Yellow | Green,
		Cool = Green | Blue | Purple,
		Any = White | Grey | Black | Warm | Cool | Brown
	}
	[System.Flags]
	public enum AssetTheme {
		Generic = 1
	}

	public string path = "";
	public string name = "";
}

public class AssetThreadInfo {
	public AAssetDesc desc;
	public bool taskComplete = false;

	public AssetThreadInfo(AAssetDesc desc)
	{
		this.desc = desc;
	}
}