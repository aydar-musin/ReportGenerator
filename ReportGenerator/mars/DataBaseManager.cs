using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace KonturGeneralInfoParser
{
    static class DataBaseManager
    {
        private static string connectionString = ConfigurationManager.AppSettings["database"];

        private enum Table { Main, OtherNames, OtherAddresses };

        public static void InsertData(CompanyInfo companyInfo)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                insertInTable(connection, companyInfo, Table.Main);

                foreach (var name in companyInfo.OtherNames)
                    insertInTable(connection, companyInfo, Table.OtherNames, name);

                foreach (var address in companyInfo.OtherAddresses)
                    insertInTable(connection, companyInfo, Table.OtherAddresses, address);
            }
        }

        private static void insertInTable(SqlConnection connection, CompanyInfo companyInfo, Table tablename, ValueWithDate otherValue = null)
        {
            try
            {
                SqlParameter[] parameters;

                if (otherValue == null)
                    parameters = getParameters(companyInfo);
                else
                    parameters = getParameters(tablename, companyInfo.OGRN, otherValue);

                string query = getQuery(tablename, parameters);
                var command = new SqlCommand(query, connection);
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot insert duplicate key"))
                {
                    string message = ex.Message.Split('.')[3].Trim() + ".";
                    Console.WriteLine(message);
                }
                else
                    throw;
            }
        }

        private static SqlParameter[] getParameters(CompanyInfo companyInfo)
        {
            var parameters = new List<SqlParameter>();

            if (companyInfo.OGRN != 0)
                parameters.Add(new SqlParameter("OGRN", companyInfo.OGRN) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.INN != 0)
                parameters.Add(new SqlParameter("INN", companyInfo.INN) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.KPP != 0)
                parameters.Add(new SqlParameter("KPP", companyInfo.KPP) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.OKPO != 0)
                parameters.Add(new SqlParameter("OKPO", companyInfo.OKPO) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.BailiffsExist != false)
                parameters.Add(new SqlParameter("BailiffsExist", companyInfo.BailiffsExist) { SqlDbType = SqlDbType.Bit });
            if (companyInfo.ArbitrationExists != false)
                parameters.Add(new SqlParameter("ArbitrationExists", companyInfo.ArbitrationExists) { SqlDbType = SqlDbType.Bit });
            if (companyInfo.ContractsExist != false)
                parameters.Add(new SqlParameter("ContractsExist", companyInfo.ContractsExist) { SqlDbType = SqlDbType.Bit });
            if (companyInfo.LicensiesExist != false)
                parameters.Add(new SqlParameter("LicensiesExist", companyInfo.LicensiesExist) { SqlDbType = SqlDbType.Bit });
            if (companyInfo.TrademarksExist != false)
                parameters.Add(new SqlParameter("TrademarksExist", companyInfo.TrademarksExist) { SqlDbType = SqlDbType.Bit });
            if (!string.IsNullOrWhiteSpace(companyInfo.Address))
                parameters.Add(new SqlParameter("Address", companyInfo.Address) { SqlDbType = SqlDbType.NVarChar });
            if (companyInfo.AddressAddedDate != default(DateTime))
                parameters.Add(new SqlParameter("AddressAddedDate", companyInfo.AddressAddedDate) { SqlDbType = SqlDbType.Date });
            if (companyInfo.ManagerAddedDate != default(DateTime))
                parameters.Add(new SqlParameter("ManagerAddedDate", companyInfo.ManagerAddedDate) { SqlDbType = SqlDbType.Date });
            if (companyInfo.FoundersCount != 0)
                parameters.Add(new SqlParameter("FoundersCount", companyInfo.FoundersCount) { SqlDbType = SqlDbType.BigInt });
            if (!string.IsNullOrWhiteSpace(companyInfo.MainActiviyCode))
                parameters.Add(new SqlParameter("MainActiviyCode", companyInfo.MainActiviyCode) { SqlDbType = SqlDbType.NVarChar });
            if (companyInfo.ArbitrDefendantCount != 0)
                parameters.Add(new SqlParameter("ArbitrDefendantCount", companyInfo.ArbitrDefendantCount) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.ArbitrPlaintiffCount != 0)
                parameters.Add(new SqlParameter("ArbitrPlaintiffCount", companyInfo.ArbitrPlaintiffCount) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.ArbitrOtherCount != 0)
                parameters.Add(new SqlParameter("ArbitrOtherCount", companyInfo.ArbitrOtherCount) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.ArbitrBankruptcyCount != 0)
                parameters.Add(new SqlParameter("ArbitrBankruptcyCount", companyInfo.ArbitrBankruptcyCount) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.WonContractsCount != 0)
                parameters.Add(new SqlParameter("WonContractsCount", companyInfo.WonContractsCount) { SqlDbType = SqlDbType.BigInt });
            if (companyInfo.PlacedContractsCount != 0)
                parameters.Add(new SqlParameter("PlacedContractsCount", companyInfo.PlacedContractsCount) { SqlDbType = SqlDbType.BigInt });

            return parameters.ToArray();
        }

        private static SqlParameter[] getParameters(Table tableName, long ogrn, ValueWithDate otherValue)
        {
            var parameters = new List<SqlParameter>();

            parameters.Add(new SqlParameter("OGRN", ogrn));

            if (tableName == Table.OtherNames)
            {
                parameters.Add(new SqlParameter("Name", otherValue.Value));
                if (otherValue.AddedDate != default(DateTime))
                    parameters.Add(new SqlParameter("AddedDate", otherValue.AddedDate));
            }
            else if (tableName == Table.OtherAddresses)
            {
                parameters.Add(new SqlParameter("Address", otherValue.Value));
                if (otherValue.AddedDate != default(DateTime))
                    parameters.Add(new SqlParameter("AddedDate", otherValue.AddedDate));
            }

            return parameters.ToArray();
        }

        private static string getQuery(Table tableName, SqlParameter[] parameters)
        {
            string query = string.Format("INSERT INTO {0} (", tableName.ToString());

            foreach (var parameter in parameters)
                if (parameter != parameters.Last())
                    query += parameter.ParameterName + ", ";
                else
                    query += parameter.ParameterName + ") ";

            query += "VALUES (";

            foreach (var parameter in parameters)
                if (parameter != parameters.Last())
                    query += "@" + parameter.ParameterName + ", ";
                else
                    query += "@" + parameter.ParameterName + ") ";

            return query;
        }
    }
}
