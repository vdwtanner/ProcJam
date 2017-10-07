using System.Data;
using Mono.Data.SqliteClient;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Streamlines Connections to SQLite databases and aids in accessing / writing to them.
/// </summary>
public class SQLiteConnection {
	private const string connectionStringPrefix = "URI=file:";
	public string databaseName { get; private set; }
	public IDbConnection dbConnection { get; private set; }
	private IDbCommand dbCommand;
	private IDbTransaction dbTransaction;

	public bool connectionOpened = false;

	public enum SQL_TYPE {
		INT,
		TINYINT,
		SMALLINT,
		FLOAT,
		VARCHAR,
		TEXT,
		BLOB
	}

	~SQLiteConnection()
	{
		Shutdown();
	}

	#region Connections

	/// <summary>
	/// Create a connection to the specified database
	/// </summary>
	/// <param name="databasePath">the path to the database from your assets folder. Include the .db extension.</param>
	/// <returns>test</returns>
	public bool ConnectToDatabase(string databasePath)
	{
		if(dbConnection != null)
		{
			Shutdown();
		}
		this.databaseName = databasePath;
		string fullConnectionString = connectionStringPrefix + Application.dataPath + "/" + databasePath;
		Debug.Log(fullConnectionString);
		dbConnection = new SqliteConnection(fullConnectionString);
		dbCommand = dbConnection.CreateCommand();
		return true;
	}

	public void Shutdown()
	{
		dbConnection.Dispose();
		dbCommand.Dispose();
		if(dbTransaction!=null)
			dbTransaction.Dispose();
	}

	public void OpenConnection()
	{
		dbConnection.Open();
		connectionOpened = true;
	}

	public void CloseConnection()
	{
		dbConnection.Close();
		connectionOpened = false;
	}
#endregion

	#region Transactions
	public void BeginTransaction()
	{
		dbTransaction = dbConnection.BeginTransaction();
	}

	public void CancelTransaction()
	{
		dbTransaction.Rollback();
	}

	public void CommitTransaction()
	{
		dbTransaction.Commit();
	}
	#endregion

	#region Queries

	public string BuildQuery(string select, string from, string where = "")
	{
		string query = "SELECT `" + select + "` FROM `" + from + "` " + (where != "" ? ("WHERE " + where) : "");
#if DEBUG_BUILD_VERBOSE
		Debug.Log("Built Query: " + query);
#endif
		return query;
	}

	/// <summary>
	/// Streamlines process of executing a query. Will return a IDataReader to access the data.
	/// </summary>
	/// <param name="query">SQL query string. Use BuildQuery for convenience when performing standard queries</param>
	/// <returns>A reader. Null if the connection isn't open</returns>
	public IDataReader ExecuteQuery(string query)
	{
		if (!connectionOpened)
		{
			return null;
		}
		dbCommand.CommandText = query;
		return dbCommand.ExecuteReader();
	}
	#endregion

	/// <summary>
	/// Handles execution of commands. Will automatically open and close a connection if the connection isn't already open
	/// </summary>
	/// <param name="command"></param>
	/// <returns>Result of the executing command</returns>
	public int ExecuteCommand(string command)
	{
		if (!connectionOpened)
		{
			dbConnection.Open();
		}

		dbCommand.CommandText = command;
#if DEBUG_BUILD_VERBOSE
		Debug.Log("Executing command: " + command);
#endif
		int result = dbCommand.ExecuteNonQuery();

		if (!connectionOpened)
		{
			dbConnection.Close();
		}
		return result;
	}
}

public class SQLiteTableBuilder {
	private string commandString;
	private bool addedAColumn = false;

	/// <summary>
	/// Begin creating a new table
	/// </summary>
	/// <param name="name">Name of the table</param>
	/// <param name="temporary">Is it a temporary table? Defaults to false</param>
	/// <param name="ifNotExists">Only create this table if it doesn't exist? Defaults to true</param>
	/// <returns>A table builder</returns>
	public SQLiteTableBuilder CreateTable(string name, bool temporary = false, bool ifNotExists = true)
	{
		commandString = "CREATE " + (temporary ? "TEMP " : "") + "TABLE " + (ifNotExists ? "IF NOT EXISTS ": "") + name + " ";

		commandString += "(";
		addedAColumn = false;
		return this;
	}

	/// <summary>
	/// Add a column to the creation string
	/// </summary>
	/// <param name="name">The name of the column</param>
	/// <param name="type">The column type</param>
	/// <param name="columnConstraint">A constraint such as "NOT NULL"</param>
	/// <param name="size">Size is only used for VARCHAR and determines max number of characters</param>
	/// <returns></returns>
	public SQLiteTableBuilder AddColumn(string name, SQLiteConnection.SQL_TYPE type, string columnConstraint = "", int size = 20)
	{
		if (addedAColumn)
		{
			commandString += ", ";
		}
		string typeString = type.ToString() + (type == SQLiteConnection.SQL_TYPE.VARCHAR ? ("(" + size + ")") : "");
		commandString += name + " " + typeString + " " + columnConstraint;
		addedAColumn = true;
		return this;
	}

	public string BuildCommand()
	{
		commandString += ");";
		return commandString;
	}
}

public class SQLiteInsertBuilder {
	private string tableName;
	private List<string> columnNames;
	private List<string> values;

	public SQLiteInsertBuilder(string tableName)
	{
		this.tableName = tableName;
		columnNames = new List<string>();
		values = new List<string>();
	}

	/// <summary>
	/// Add a column, value pair
	/// </summary>
	/// <param name="columnName"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public SQLiteInsertBuilder Insert(string columnName, string value)
	{
		columnNames.Add(columnName);
		string strVal = "'" + value.ToString() + "'";
		values.Add(strVal);
		return this;
	}

	/// <summary>
	/// Add a column, value pair
	/// </summary>
	/// <typeparam name="T">Only pass value types</typeparam>
	/// <param name="columnName"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public SQLiteInsertBuilder Insert<T>(string columnName, T value) where T : struct	//Only allow value types
	{
		columnNames.Add(columnName);
		if (value is bool)//Bools are represented as 1 or 0 in DB
		{
			bool b = value.ToString().Equals("True");
			values.Add((b ? 1 : 0).ToString());
		}
		else
		{
			values.Add(value.ToString());
		}
		return this;
	}

	/// <summary>
	/// Compile the insert command
	/// </summary>
	/// <returns></returns>
	public string BuildCommand()
	{
		string str = "INSERT INTO " + tableName + " (";
		//Add column names
		for (int x = 0; x < columnNames.Count; x++)
		{
			str += columnNames[x] + (x < columnNames.Count - 1 ? ", " : "");
		}
		str += ") VALUES (";

		//Add values
		for (int x = 0; x < values.Count; x++)
		{
			str += values[x] + (x < values.Count - 1 ? ", " : "");
		}
		str += ");";
		return str;
	}
}