using System;
using System.Data;
using System.IO;
using System.Web;

namespace SPV_Loader
{
    class Currency
    {
        public static string GetCurrency(string countryCode)
        {
            string currency = "";

            // Load Lookup Table
            //string baseName = System.Environment.CurrentDirectory;
            string CSVFilePathName = HttpContext.Current.Server.MapPath("~//App_Data//CurrencyLookup.csv");
            string[] Lines = File.ReadAllLines(CSVFilePathName);
            string[] Fields;
            Fields = Lines[0].Split(new char[] { ',' });
            int Cols = Fields.GetLength(0);
            DataTable CodesLookup = new DataTable();
            //1st row must be column names; force lower case to ensure matching later on.
            for (int i = 0; i < Cols; i++)
                CodesLookup.Columns.Add(Fields[i].ToLower(), typeof(string));
            DataRow Row;
            for (int i = 1; i < Lines.GetLength(0); i++)
            {
                Fields = Lines[i].Split(new char[] { ',' });
                Row = CodesLookup.NewRow();
                for (int f = 0; f < Cols; f++) Row[f] = Fields[f];
                CodesLookup.Rows.Add(Row);
            }

            for (int j = 0; j < CodesLookup.Rows.Count; j++)
            {
                if (countryCode == CodesLookup.Rows[j][1].ToString())
                {
                    currency = CodesLookup.Rows[j][3].ToString();
                }
            }

            return currency;
        }
    }
}
