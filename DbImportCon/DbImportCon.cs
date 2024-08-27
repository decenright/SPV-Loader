using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;

namespace SPV_Loader.DbImportCon
{
    public class DbImportCon
    {
        public void clearOrder()
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["ConString"].ToString()))
            {
                con.Open();
                SqlCommand cmd1 = new SqlCommand("Delete from Orders", con);
                SqlCommand cmd2 = new SqlCommand("DBCC CHECKIDENT ('ImportOrder', RESEED, 0) ", con);
                SqlCommand cmd3 = new SqlCommand("DBCC CHECKIDENT ('NullAddress', RESEED, 0) ", con);
                SqlCommand cmd4 = new SqlCommand("Delete from NullAddress", con);
                cmd1.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                cmd3.ExecuteNonQuery();
                cmd4.ExecuteNonQuery();
                con.Close();
            }
        }

        public void ImportOrder(DataTable table)
        {

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["ConString"].ToString()))
            {
                con.Open();

                foreach (DataRow row in table.Rows)
                {
                    var projectId = row[0].ToString();
                    var itemPartNumber = row[27].ToString();
                    var date = Convert.ToDateTime(row[43]);
                    var dateNeeded1 = row[8].ToString().Split(' ');
                    var dateNeeded = Convert.ToDateTime(dateNeeded1[0]);
                    var shipToContactName = row[15].ToString();
                    var orderedQuantity = row[30].ToString();
                    var lineNumber = row[34].ToString();
                    var itemDescriptionEnglish = row[29].ToString();
                    var unitCost = row[36].ToString();
                    var specialInstructions = row[26].ToString();
                    var shipToCompanyName = row[16].ToString();
                    var shipToContactPhone = row[23].ToString();
                    var shipToAddress1 = row[17].ToString();
                    var shipToAddress2 = row[18].ToString();
                    var shipToAddress3 = row[19].ToString();
                    var shipToCity = row[20].ToString();
                    var shipToState = row[21].ToString();
                    var shipToPostalCode = row[22].ToString();
                    var shipToCountry = row[14].ToString();
                    var shipToCountryCode = row[42].ToString();
                    var approvalCostCenter = row[12].ToString();
                    var HSCode = row[33].ToString();
                    var countryOfOriginCode = row[40].ToString();
                    var DHLHSCode = row[41].ToString();
                    var concatKitReq = row[44].ToString();
                    var weight = row[37].ToString();
                    var isNewPart = row[45].ToString();
                    var targetAccount = row[46].ToString();

                    if (shipToAddress1 != "")
                    {
                        string query = "INSERT INTO ImportOrder(ProjectId,ItemPartNumber,Date,DateNeeded,ShipToContactName, OrderedQuantity, LineNumber,ItemDescriptionEnglish, UnitCost, SpecialInstructions, ShipToCompany, ShipToContactPhone, ShipToAddress1, ShipToAddress2,ShipToAddress3, ShipToCity, ShipToState, ShipToPostalCode,ShipToCountry,ShipToCountryCode,ApprovalCostCenter, HSCode, CountryOfOrigin,DHL_HSCode,Concat_Kit_Req_Aprv_CC, Weight, IsNewPart, TargetAccount) VALUES(@Param1,@Param2,@Param3,@Param4,@Param5,@Param6,@Param7,@Param8,@Param9,@Param10,@Param11,@Param12,@Param13,@Param14,@Param15,@Param16,@Param17,@Param18,@Param19,@Param20,@Param21, @Param22, @Param23, @Param24, @Param25, @Param26,@Param27,@Param28)";
                        using (SqlCommand command = new SqlCommand(query, con))
                        {
                            // Set parameter values
                            command.Parameters.AddWithValue("@Param1", projectId);
                            command.Parameters.AddWithValue("@Param2", itemPartNumber);
                            command.Parameters.AddWithValue("@Param3", date);
                            command.Parameters.AddWithValue("@Param4", dateNeeded);
                            command.Parameters.AddWithValue("@Param5", shipToContactName); //
                            command.Parameters.AddWithValue("@Param6", orderedQuantity);
                            command.Parameters.AddWithValue("@Param7", lineNumber);
                            command.Parameters.AddWithValue("@Param8", itemDescriptionEnglish); //
                            command.Parameters.AddWithValue("@Param9", unitCost);
                            command.Parameters.AddWithValue("@Param10", specialInstructions);
                            command.Parameters.AddWithValue("@Param11", shipToCompanyName);
                            command.Parameters.AddWithValue("@Param12", shipToContactPhone);
                            command.Parameters.AddWithValue("@Param13", shipToAddress1);
                            command.Parameters.AddWithValue("@Param14", shipToAddress2);
                            command.Parameters.AddWithValue("@Param15", shipToAddress3);
                            command.Parameters.AddWithValue("@Param16", shipToCity);
                            command.Parameters.AddWithValue("@Param17", shipToState);
                            command.Parameters.AddWithValue("@Param18", shipToPostalCode);
                            command.Parameters.AddWithValue("@Param19", shipToCountry);
                            command.Parameters.AddWithValue("@Param20", shipToCountryCode);
                            command.Parameters.AddWithValue("@Param21", approvalCostCenter);
                            command.Parameters.AddWithValue("@Param22", HSCode);
                            command.Parameters.AddWithValue("@Param23", countryOfOriginCode);
                            command.Parameters.AddWithValue("@Param24", DHLHSCode);
                            command.Parameters.AddWithValue("@Param25", concatKitReq);
                            command.Parameters.AddWithValue("@Param26", weight);
                            command.Parameters.AddWithValue("@Param27", isNewPart);
                            command.Parameters.AddWithValue("@Param28", targetAccount);

                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string query1 = "INSERT INTO NullAddress(ProjectId) VALUES(@Param1)";

                        using (SqlCommand command = new SqlCommand(query1, con))
                        {
                            command.Parameters.AddWithValue("@Param1", projectId);
                            command.ExecuteNonQuery();
                        }
                    }

                }
                con.Close();
            }
        }
    }
}