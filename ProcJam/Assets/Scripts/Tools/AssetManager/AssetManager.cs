using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using System.Linq;

public class AssetManager {
	protected SQLiteConnection sqlCon;
	protected bool tablesReady = false;

	private static AssetManager s_Instance;


	public static AssetManager Instance
	{
		get
		{
			if (s_Instance != null)
			{
				return s_Instance;
			}
			s_Instance = new AssetManager();
			return s_Instance;
		}
	}

	protected AssetManager()
	{
		Initialize();
	}

	public static void TerminateConnection()
	{
		if (s_Instance != null)
		{
			s_Instance.Shutdown();
		}
	}

	#region AssetAdders

	public void AddAssetAsync(AAssetDesc desc)
	{
		AssetThreadInfo threadInfo = new AssetThreadInfo(desc);
		ThreadPool.QueueUserWorkItem(AddAssetTask, threadInfo);
#if DEBUG_BUILD_VERBOSE
		Debug.Log("Spun off AddAsset task thread for " + desc.name);
#endif
	}

	protected void AddAssetTask(object threadInfo)
	{
		Thread thread = Thread.CurrentThread;
		Debug.Assert(thread.IsBackground, "DB threads must work in background");

		AssetThreadInfo info = threadInfo as AssetThreadInfo;
		//Wait until connection is closed and the table is ready 
		while (!tablesReady || sqlCon.connectionOpened)
		{
			//Debug.Log("Desc.Name = " + info.desc.name + " :: TablesReady = " + tablesReady + " :: sqlCon.Open = " + sqlCon.connectionOpened);
			Thread.Sleep(10);
		}
		
		string command = MakeInsertCmdFromDesc(info.desc);

		sqlCon.OpenConnection();
		sqlCon.BeginTransaction();
		sqlCon.ExecuteCommand(command);
		sqlCon.CommitTransaction();
		sqlCon.CloseConnection();
		info.taskComplete = true;

#if DEBUG_BUILD_VERBOSE
		string className = info.desc.GetType().Name;
		if (className.Substring(className.Length - 4) == "Desc")
		{
			className = className.Substring(0, className.Length - 4);
		}
		Debug.Log("Added " + info.desc.name + " to the " + className + " table");
#endif
	}

	#endregion

	#region AssetGetters

	/// <summary>
	/// Coroutine that gets an asset asynchronously. Yields until the asset has been retrieved.
	/// </summary>
	/// <param name="desc">Descriptor for the Asset</param>
	/// <param name="gameObject">The GameObject that was retrieved</param>
	/// <returns></returns>
	public IEnumerator GetAssetAsync(AAssetDesc desc, out GameObject gameObject)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Coroutine that gets a number of assets that match the descriptor asynchronously. Yields until the assets have been retrieved.
	/// </summary>
	/// <param name="desc">Descriptor for the Assets</param>
	/// <param name="count">How many assets to retrieve</param>
	/// <param name="gameObjects">A List to fill with retrieved Game Objects</param>
	/// <returns></returns>
	public IEnumerator GetAssetsAsync(AAssetDesc desc, int count, out List<GameObject> gameObjects)
	{
		throw new NotImplementedException();
	}
	#endregion

	#region Initialization

	/// <summary>
	/// Initial setup. Will start up a connection to the asset database
	/// </summary>
	public virtual void Initialize()
	{
		sqlCon = new SQLiteConnection();
		sqlCon.ConnectToDatabase("AssetDatabase.db");
		tablesReady = false;

		//Construct tables for all assetDescriptions
		ThreadPool.QueueUserWorkItem(ConstructTables);
		Debug.Log("Started thread to create Tables");
	}

	/// <summary>
	/// Asynchronous construction call to make sure that the tables exist
	/// </summary>
	/// <param name="state"></param>
	protected void ConstructTables(object state)
	{
		Thread thread = Thread.CurrentThread;
		Debug.Assert(thread.IsBackground, "DB threads must work in background");

		var descriptions = typeof(AAssetDesc).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(AAssetDesc)));
		sqlCon.OpenConnection();

		foreach (Type descriptionType in descriptions)
		{
			//Instantiate a descriptor so that we can create a table from it.
			var ctors = descriptionType.GetConstructors();
			AAssetDesc desc = ctors[0].Invoke(new object[] { }) as AAssetDesc;

			string command = MakeTableCmdFromDesc(desc);
			
			sqlCon.ExecuteCommand(command);
		}
		sqlCon.CloseConnection();
		tablesReady = true;
		Debug.Log("Finished Building Tables");
	}
#endregion

	/// <summary>
	/// Run the shutdown sequence. Will disconnect from the asset database
	/// </summary>
	protected virtual void Shutdown()
	{
		sqlCon.Shutdown();
	}
	#region Reflection
	/// <summary>
	/// Make a Create Table command from a IDbDesc (Database Descriptor).
	/// ints will be treated as INTEGER, bool as TINYINT, float as REAL, and string as TEXT (unless labeled with VARCHAR attribute)
	/// I would prefer to just pass types for this function, but I haven't figured out a good way to ensure that the type implements
	/// IAssetDesc at compile time yet.
	/// </summary>
	/// <param name="desc">The descriptor to create a table for</param>
	/// <returns>The command string</returns>
	public static string MakeTableCmdFromDesc(IAssetDesc desc)
	{
		string className = desc.GetType().Name;
		if (className.Substring(className.Length - 4) == "Desc")
		{
			className = className.Substring(0, className.Length - 4);
		}
		string cmd = "CREATE TABLE  IF NOT EXISTS " + className + " (";
		var fields = desc.GetType().GetFields();
		bool firstIteration = true;
		//Create column descriptions
		foreach (var field in fields)
		{
			if (!field.IsPublic)
			{
				continue;
			}
			if (firstIteration)
			{
				firstIteration = false;
			}
			else
			{
				cmd += ", ";
			}
			if (field.FieldType == typeof(int) || field.FieldType.IsEnum)
			{
				cmd += field.Name + " INTEGER";
			}
			else if (field.FieldType == typeof(bool))
			{
				cmd += field.Name + " TINYINT";
			}
			else if (field.FieldType == typeof(float))
			{
				cmd += field.Name + " REAL";
			}
			else if (field.FieldType == typeof(string))
			{
				object[] attributes = field.GetCustomAttributes(typeof(VarcharAttribute), true);
				if (attributes.Length > 0)
				{
					VarcharAttribute varchar = (VarcharAttribute)attributes[0];
					cmd += field.Name + " VARCHAR(" + varchar.length.ToString() + ")";
				}
				else
				{
					cmd += field.Name + " TEXT";
				}
			}
		}
		cmd += ");";
		return cmd;
	}

	/// <summary>
	/// Make a INSERT command from an IDbDesc (Database descriptor)
	/// </summary>
	/// <param name="desc">The descriptor to insert</param>
	/// <returns>The command string</returns>
	public static string MakeInsertCmdFromDesc(IAssetDesc desc)
	{
		//"INSERT INTO Trainers VALUES (1, \"" + trainerName + "\")";
		string className = desc.GetType().Name;
		if (className.Substring(className.Length - 4) == "Desc")
		{
			className = className.Substring(0, className.Length - 4);
		}
		string cmd = "INSERT INTO " + className + "(";
		var fields = desc.GetType().GetFields();

		//generate col - value pairs
		List<string> cols = new List<string>();
		List<string> vals = new List<string>();
		foreach (var field in fields)
		{
			if (!field.IsPublic)
			{
				continue;
			}
			cols.Add(field.Name);
			if (field.GetValue(desc) == null)
			{
				vals.Add("NULL");
				continue;
			}
			if (field.FieldType == typeof(bool))
			{
				if ((bool)field.GetValue(desc))
				{
					vals.Add("1");
				}
				else
				{
					vals.Add("0");
				}
			}
			else if (field.FieldType == typeof(string))
			{
				vals.Add("'" + field.GetValue(desc).ToString() + "'");
			}
			else if (field.FieldType.IsEnum)
			{
				vals.Add(((int)field.GetValue(desc)).ToString());
			}
			else
			{
				vals.Add(field.GetValue(desc).ToString());

			}
		}

		//specify column mapping
		bool firstIteration = true;
		foreach (string col in cols)
		{
			if (firstIteration)
			{
				firstIteration = false;
			}
			else
			{
				cmd += ", ";
			}
			cmd += col;
		}
		cmd += ") VALUES (";
		firstIteration = true;

		//List the values
		foreach (string val in vals)
		{
			if (firstIteration)
			{
				firstIteration = false;
			}
			else
			{
				cmd += ", ";
			}
			cmd += val;
		}

		cmd += ");";
		return cmd;
	}
	#endregion

}

public class VarcharAttribute : Attribute {
	public int length = 20;
	public VarcharAttribute(int length = 20)
	{
		this.length = length;
	}
}

/// <summary>
/// Marks a class as a Database Descriptor. The Database class will be able to create tables and queries automatically based on the descriptor.
/// </summary>
public interface IAssetDesc { };

[System.Serializable]
public abstract class AAssetDesc : IAssetDesc {
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
	}
	[System.Flags]
	public enum AssetTheme {
		Generic = 1
	}
	[HideInInspector]
	public string path = "";
	[Varchar(40)]
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

[Serializable]
public class PropAssetDesc : AAssetDesc {

	public enum PropType {
		Prop,
		Lever,
		Button,
	}

	public PropType propType = PropType.Prop;
	public AssetSize size = AssetSize.Medium;
	public AssetColor primaryColor = AssetColor.White;
	public AssetColor secondaryColor = 0;
	public bool emitsLight = false;
	public AssetColor lightColor = 0;
	public AssetTheme theme = AssetTheme.Generic;
}

[Serializable]
public class WeaponAssetDesc : AAssetDesc {

}