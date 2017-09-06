using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SkyCore.Database
{

	public class DatabaseSource
	{

		public MySqlConnection GetConnection()  
		{
			var myConnectionString = "server=108.170.62.98;uid=root;pwd=kjArc4quuqVuu9Hk;database=SkyCorePE";

			var conn = new MySqlConnection { ConnectionString = myConnectionString };
			conn.Open();

			return conn;
		}

		public void Query(string queryString)
		{
			MySqlConnection connection = null;
			try
			{
				connection = GetConnection();

				MySqlCommand command = connection.CreateCommand();

				//command.CommandText = 
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			finally
			{
				if (connection != null)
				{
					connection.Dispose();
				}
			}
		}
		
	}
}
