using System;
using System.CodeDom;
using System.Collections.Generic;
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

		public MySqlConnection GetConnection()  
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

}
