using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SkyCore.Database
{

	public class DatabaseAction
	{

		public delegate void QueryAction(MySqlCommand command);

		public delegate void ResultAction(MySqlDataReader reader);

		public static MySqlConnection GetConnection()  
		{
			//Connection String
			var myConnectionString = "server=127.0.0.1;uid=root;pwd=password;database=SkyCorePE";

			var conn = new MySqlConnection { ConnectionString = myConnectionString };
			conn.Open();

			return conn;
		}

		public void Execute(string queryString, QueryAction queryAction, Delegate postDelegate)
		{
			_Query(queryString, queryAction, null, postDelegate, false);
		}

		public void Query(string queryString, QueryAction queryAction, ResultAction resultAction, Delegate postDelegate)
		{
			_Query(queryString, queryAction, resultAction, postDelegate, true);
		}

		private void _Query(string queryString, QueryAction queryAction, ResultAction resultAction, Delegate postDelegate, bool query)
		{
			MySqlConnection connection = null;
			MySqlCommand command = null;
			MySqlDataReader reader = null;
			try
			{
				connection = GetConnection();

				command = new MySqlCommand(
					queryString,
					connection
				);

				queryAction?.Invoke(command);

				if (query)
				{
					reader = command.ExecuteReader();

					while (reader.Read())
					{
						resultAction?.Invoke(reader);
					}
				}
				else
				{
					command.ExecuteNonQuery();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				reader?.Dispose();
				command?.Dispose();
				connection?.Dispose();

				try
				{
					postDelegate?.DynamicInvoke();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}
		
	}

	public class DatabaseBatch<T>
	{

		public delegate void BatchParametersAction(MySqlParameterCollection parameters);

		//True if this item should be added,
		//False if not.
		public delegate bool BatchQueryAction(DataRow dataRow, T batchItem);

		//

		private readonly string _queryString;

		private readonly string _tableName;

		private readonly BatchParametersAction _batchParametersAction;

		private readonly BatchQueryAction _batchQueryAction;

		private readonly Delegate _postDelegate;

		private readonly IEnumerable<T> _batchCollection;

		public DatabaseBatch(string queryString, string tableName, BatchParametersAction batchParametersAction, BatchQueryAction batchQueryAction, Delegate postDelegate, IEnumerable<T> batchCollection)
		{
			_queryString = queryString;
			_tableName = tableName;
			_batchParametersAction = batchParametersAction;
			_batchQueryAction = batchQueryAction;
			_postDelegate = postDelegate;

			_batchCollection = batchCollection;
		}

		public void ExecuteBatch()
		{
			if (_batchQueryAction == null)
			{
				throw new Exception("Unable to process batch without a valid BatchQueryAction");
			}

			if (_batchParametersAction == null)
			{
				throw new Exception("Unable to process batch without a valid BatchParametersAction");
			}

			MySqlConnection connection = null;
			MySqlDataAdapter batchAdapter = null;
			try
			{
				connection = DatabaseAction.GetConnection();

				MySqlCommand command = new MySqlCommand(_queryString, connection);
				batchAdapter = new MySqlDataAdapter {InsertCommand = command};

				//TODO: Allow different query types
				batchAdapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;

				DataTable dataTable = new DataTable(_tableName);

				_batchParametersAction.Invoke(command.Parameters);
				foreach (MySqlParameter parameter in command.Parameters)
				{
					DataColumn dataColumn = new DataColumn(parameter.ParameterName.Replace("@", ""), SqlHelper.GetTypeFromSqlType(parameter.MySqlDbType));

					dataTable.Columns.Add(dataColumn);
				}
				
				foreach (T batchItem in _batchCollection)
				{
					DataRow dataRow = dataTable.NewRow();
					if (_batchQueryAction.Invoke(dataRow, batchItem))
					{
						dataTable.Rows.Add(dataRow);
					}
				}

				batchAdapter.UpdateBatchSize = 0; //Max size supported by db

				batchAdapter.Update(dataTable);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				batchAdapter?.Dispose();
				connection?.Dispose();

				try
				{
					_postDelegate?.DynamicInvoke();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

	}

	public static class SqlHelper
	{
		private static readonly Dictionary<Type, MySqlDbType> TypeToSQlTypeMap = new Dictionary<Type, MySqlDbType>();
		private static readonly Dictionary<MySqlDbType, Type> SqlTypeToTypeMap = new Dictionary<MySqlDbType, Type>();

		// Create and populate the dictionary in the static constructor
		static SqlHelper()
		{
			AddEntry(typeof(string), MySqlDbType.VarChar);
			AddEntry(typeof(char[]), MySqlDbType.VarChar);
			AddEntry(typeof(byte), MySqlDbType.Int16);
			AddEntry(typeof(short), MySqlDbType.Int24);
			AddEntry(typeof(int), MySqlDbType.Int32);
			AddEntry(typeof(long), MySqlDbType.Int64);
			AddEntry(typeof(bool), MySqlDbType.Bit);
			AddEntry(typeof(DateTime), MySqlDbType.DateTime);
			AddEntry(typeof(decimal), MySqlDbType.Decimal);
			AddEntry(typeof(float), MySqlDbType.Float);
			AddEntry(typeof(double), MySqlDbType.Double);
			AddEntry(typeof(TimeSpan), MySqlDbType.Time);
			//TODO: Remaining Types
		}

		private static void AddEntry(Type type, MySqlDbType sqlDbType)
		{
			//Only add the first instance of a key.
			//Higher up should be more important/used
			if (!TypeToSQlTypeMap.ContainsKey(type))
			{
				TypeToSQlTypeMap.Add(type, sqlDbType);
			}

			if (!SqlTypeToTypeMap.ContainsKey(sqlDbType))
			{
				SqlTypeToTypeMap.Add(sqlDbType, type);
			}
		}

		
		public static MySqlDbType GetSqlTypeFromType(Type giveType)
		{
			// Allow nullable types to be handled
			giveType = Nullable.GetUnderlyingType(giveType) ?? giveType;

			if (TypeToSQlTypeMap.ContainsKey(giveType))
			{
				return TypeToSQlTypeMap[giveType];
			}

			throw new ArgumentException($"{giveType.FullName} is not a supported .NET class");
		}

		public static Type GetTypeFromSqlType(MySqlDbType giveType)
		{
			if (SqlTypeToTypeMap.ContainsKey(giveType))
			{
				return SqlTypeToTypeMap[giveType];
			}

			throw new ArgumentException($"{giveType} is not a supported MySqlDbType class");
		}

	}

}
