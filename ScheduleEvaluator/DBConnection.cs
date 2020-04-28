using System.Data.SqlClient;
using System.Data;
using System.Text;

namespace ScheduleEvaluator
{
    public class DBConnection
    {
        SqlConnection myConnection;      //Declare the SQL connection to Database

        #region SQL Stuff
        //------------------------------------------------------------------------------
        // concise code to execute queries
        // 
        // 
        //------------------------------------------------------------------------------

        public DataTable ExecuteToDT(string query)
        {
            OpenSQLConnection();
            SqlCommand cmd = new SqlCommand(query, myConnection);
            DataTable dt = new DataTable();
            using (var con = myConnection)
            {
                using (var command = new SqlCommand(query))
                {
                    myConnection.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        dt.Load(dr);
                    }
                }
            }
            myConnection.Close();
            return dt;
        }

        public string ExecuteToString(string query)
        {

            OpenSQLConnection();
            SqlCommand cmd = new SqlCommand(query, myConnection);
            myConnection.Open();
            var result = new StringBuilder();
            var reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                result.Append("[]");
            }
            else
            {
                while (reader.Read())
                {
                    result.Append(reader.GetValue(0).ToString());
                }
            }
            myConnection.Close();
            return result.ToString();
        }
        //------------------------------------------------------------------------------
        // sql connection
        // 
        // 
        //------------------------------------------------------------------------------
        private void OpenSQLConnection()
        {

            myConnection = new SqlConnection("");
        }
        #endregion

    }
}
