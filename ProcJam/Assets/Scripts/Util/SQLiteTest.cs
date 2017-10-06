using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SQLiteTest : MonoBehaviour {
	public string dbName = "AssetDatabase.db";

	private SQLiteConnection sqlCon;

	// Use this for initialization
	void Start () {
		sqlCon = new SQLiteConnection();
		if (!sqlCon.ConnectToDatabase(dbName))
		{
			Debug.LogError("Connection Failure");
			return;
		}
		Test();
	}
	
	void Test()
	{
		SQLiteTableBuilder tableBuilder = new SQLiteTableBuilder();
		tableBuilder.CreateTable("Props");
		tableBuilder.AddColumn("name", SQLiteConnection.SQL_TYPE.VARCHAR)
			.AddColumn("size", SQLiteConnection.SQL_TYPE.TINYINT)
			.AddColumn("color", SQLiteConnection.SQL_TYPE.VARCHAR, "", 12)
			.AddColumn("theme", SQLiteConnection.SQL_TYPE.VARCHAR)
			.AddColumn("path", SQLiteConnection.SQL_TYPE.VARCHAR, "", 50);

		SQLiteInsertBuilder insertBuilder = new SQLiteInsertBuilder("Props");
		insertBuilder.Insert("name", "stool")
			.Insert("size", 1)
			.Insert("theme", "janky")
			.Insert("path", "meh")
			.Insert("color", "blue");

		sqlCon.OpenConnection();
		sqlCon.BeginTransaction();

		int result = sqlCon.ExecuteCommand(tableBuilder.BuildCommand());
		Debug.Log(result);
		result = sqlCon.ExecuteCommand(insertBuilder.BuildCommand());
		Debug.Log(result);

		sqlCon.CommitTransaction();
		sqlCon.CloseConnection();

		
	}
}
