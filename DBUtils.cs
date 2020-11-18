using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEV_Agent_V2
{
    class DBUtils
    {
        public static SqlConnection GetDBConnection()
        {
            // string datasource = @".\SQLEXPRESS";
            string datasource = System.IO.File.ReadAllText("sql.txt");

            string database = "hev";
            string username = "sa";
            string password = "Auth=8495";

            return DBSQLServerUtils.GetDBConnection(datasource, database, username, password);
        }
    }
}
