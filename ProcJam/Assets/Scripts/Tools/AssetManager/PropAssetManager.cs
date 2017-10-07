using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class PropAssetManager : AAssetManager {
	const string TABLE_NAME = "Props";
	private static PropAssetManager s_Instance;
	

	public static PropAssetManager Instance {
		get
		{
			if(s_Instance != null)
			{
				return s_Instance;
			}
			s_Instance = new PropAssetManager();
			return s_Instance;
		}
	}

	protected override void ConstructTable(object state)
	{
		SQLiteTableBuilder tableBuilder = new SQLiteTableBuilder();
		tableBuilder.CreateTable(TABLE_NAME)
			.AddColumn("propType", SQLiteConnection.SQL_TYPE.TINYINT)
			.AddColumn("name", SQLiteConnection.SQL_TYPE.VARCHAR, "", 40)
			.AddColumn("size", SQLiteConnection.SQL_TYPE.TINYINT)
			.AddColumn("primaryColor", SQLiteConnection.SQL_TYPE.SMALLINT)
			.AddColumn("secondaryColor", SQLiteConnection.SQL_TYPE.SMALLINT)
			.AddColumn("emitsLight", SQLiteConnection.SQL_TYPE.TINYINT)
			.AddColumn("lightColor", SQLiteConnection.SQL_TYPE.SMALLINT)
			.AddColumn("theme", SQLiteConnection.SQL_TYPE.SMALLINT)
			.AddColumn("path", SQLiteConnection.SQL_TYPE.VARCHAR, "NOT NULL", 100);

		sqlCon.OpenConnection();
		sqlCon.ExecuteCommand(tableBuilder.BuildCommand());
		sqlCon.CloseConnection();

		tableReady = true;

		Debug.Log("Built table: " + TABLE_NAME);
	}

	#region AssetGetters
	public override IEnumerator GetAssetAsync(AAssetDesc desc, out GameObject gameObject)
	{
		Debug.Assert(desc is PropAssetDesc, "Must be a PropAssetDescription");

		throw new NotImplementedException();
	}

	public override IEnumerator GetAssetsAsync(AAssetDesc desc, int count, out List<GameObject> gameObjects)
	{
		Debug.Assert(desc is PropAssetDesc, "Must be a PropAssetDescription");

		throw new NotImplementedException();
	}
	#endregion

	#region Asset Adders

	public override IEnumerator AddAssetAsync(AAssetDesc desc)
	{
		Debug.Assert(desc is PropAssetDesc, "Must be PropAssetDescription");
		//Wait until connection is closed and the table is ready 
		while (!tableReady || sqlCon.connectionOpened)
		{
			yield return null;
		}
		AssetThreadInfo threadInfo = new AssetThreadInfo(desc);
		ThreadPool.QueueUserWorkItem(AddAsset, threadInfo);
	}


	public override void Shutdown()
	{
		throw new NotImplementedException();
	}

	protected override void AddAsset(object threadInfo)
	{
		Debug.Assert(threadInfo is AssetThreadInfo, "Thread info must be AssetThreadInfo!");
		AssetThreadInfo info = threadInfo as AssetThreadInfo;
		PropAssetDesc desc = info.desc as PropAssetDesc;
		SQLiteInsertBuilder insertBuilder = new SQLiteInsertBuilder(TABLE_NAME);
		insertBuilder.Insert("propType", (int)desc.propType)
			.Insert("name", desc.name)
			.Insert("size", (int)desc.size)
			.Insert("primaryColor", (int)desc.primaryColor)
			.Insert("secondaryColor", (int)desc.secondaryColor)
			.Insert("emitsLight", desc.emitsLight)
			.Insert("lightColor", (int)desc.lightColor)
			.Insert("theme", (int)desc.theme)
			.Insert("path", desc.path);

		sqlCon.OpenConnection();
		sqlCon.BeginTransaction();
		sqlCon.ExecuteCommand(insertBuilder.BuildCommand());
		sqlCon.CommitTransaction();
		sqlCon.CloseConnection();
		info.taskComplete = true;
#if DEBUG_BUILD_VERBOSE
		Debug.Log("Added " + desc.name + " to the " + TABLE_NAME + " table");
#endif
	}
#endregion
}

[Serializable]
public class PropAssetDesc : AAssetDesc {
	
	public enum PropType {
		Prop,
		Lever,
		Button,
	}

	public PropType propType = PropType.Prop;
	public AssetSize size = AssetSize.Medium;
	public AssetColor primaryColor = AssetColor.Any;
	public AssetColor secondaryColor = AssetColor.Any;
	public bool emitsLight = false;
	public AssetColor lightColor = AssetColor.Any;
	public AssetTheme theme = AssetTheme.Generic;
}