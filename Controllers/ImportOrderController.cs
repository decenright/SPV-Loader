using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net;
using SPV_Loader.Models;
using System.Web.UI;
using Microsoft.Ajax.Utilities;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using VBFileIO = Microsoft.VisualBasic.FileIO;
using System.Globalization;
using System.Deployment.Internal;
using System.Text.RegularExpressions;
using ExcelDataReader;
using System.Web.UI.WebControls;
using System.Configuration;
using System.EnterpriseServices.CompensatingResourceManager;

namespace SPV_Loader.Controllers
{
    public class ImportOrderController : Controller
    {
        private readonly string _connectionString = "Data Source=CM-APP-SVR\\SQLEXPRESS;Initial Catalog=SpvLoader;Integrated Security=true";

        [HttpPost]
        public ActionResult ImportOrder(HttpPostedFileBase postedFile, string sliderValue)
        {
            if (postedFile == null || postedFile.ContentLength == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No file uploaded.");
                ModelState.AddModelError("file", "Please select a file to upload.");
                return RedirectToAction("Index", "Athena");
            }

            try
            {
                // Save the uploaded file to a temporary path
                string tempFilePath = Server.MapPath("~/App_Data/Temp.xls");
                postedFile.SaveAs(tempFilePath);

                // Load the XML document
                XDocument xmlDoc = XDocument.Load(tempFilePath);

                // Determine the structure and count of elements in the XML
                int numOfJobs = xmlDoc.Root.Elements().Count();
                int numOfFieldsInJob = xmlDoc.Root.Elements().ElementAt(0).Elements().ElementAt(0).Elements().Count();

                // Create an array to store job information
                Order[] jobsArray = new Order[numOfJobs];

                for (int i = 0; i < numOfJobs; i++)
                {
                    // Create and fill string array with values from XML file
                    string[] values = new string[numOfFieldsInJob];

                    for (int j = 0; j < numOfFieldsInJob; j++)
                    {
                        if (xmlDoc.Root.Elements().ElementAt(i).Elements().ElementAt(0).Elements().ElementAt(j).Elements().ElementAt(0).Value == "")
                        {
                            values[j] = "N/A";
                        }
                        else
                        {
                            values[j] = xmlDoc.Root.Elements().ElementAt(i).Elements().ElementAt(0).Elements().ElementAt(j).Elements().ElementAt(0).Value;
                        }
                    }

                    // Create new order and place it in array of orders
                    jobsArray[i] = new Order((i + 1).ToString(), values);
                }

                // Convert jobsArray to DataTable
                DataTable jobs = new DataTable("Jobs");
                jobs.Columns.Add("Id");
                jobs.Columns.Add("JobNumber");
                jobs.Columns.Add("DueDate");
                jobs.Columns.Add("PurchaseOrderNumber");
                jobs.Columns.Add("PurchaseOrderLine");
                jobs.Columns.Add("SalesOrderNumber");
                jobs.Columns.Add("CustomerAccountCode");
                jobs.Columns.Add("JobQuantity");
                jobs.Columns.Add("AscmOrderId");
                jobs.Columns.Add("EndCustomer");
                jobs.Columns.Add("ActivationSystem");
                jobs.Columns.Add("ProductType");
                jobs.Columns.Add("ErpMaterialCode");
                jobs.Columns.Add("IntegratorPartId");
                jobs.Columns.Add("IntegratorID");
                jobs.Columns.Add("ActivationType");
                jobs.Columns.Add("PartNumberSku");
                jobs.Columns.Add("RetailBarcode");
                jobs.Columns.Add("RetailBarcodeType");
                jobs.Columns.Add("OCR");
                jobs.Columns.Add("Channel");

                foreach (var order in jobsArray)
                {
                    DataRow row = jobs.NewRow();
                    row["Id"] = order.Id;
                    for (int i = 0; i < order.Values.Length; i++)
                    {
                        row[i + 1] = order.Values[i];
                    }
                    jobs.Rows.Add(row);
                }

                var dach = "0";
                if(sliderValue == "IsDach")
                {
                    dach = "1";
                }

                // Delete existing data from the database tables
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    SqlCommand cmd1 = new SqlCommand("DELETE FROM AthenaJobs", con);
                    SqlCommand cmd2 = new SqlCommand("DELETE FROM ExportAthena", con);
                    SqlCommand cmd3 = new SqlCommand("DELETE FROM Dach", con);
                    SqlCommand cmd4 = new SqlCommand("DELETE FROM BHNWorkInstructions", con);
                    SqlCommand cmd5 = new SqlCommand("DBCC CHECKIDENT ('AthenaJobs', RESEED, 0) ", con);
                    SqlCommand cmd6 = new SqlCommand("DBCC CHECKIDENT ('ExportAthena', RESEED, 0) ", con);
                    SqlCommand cmd7 = new SqlCommand("DBCC CHECKIDENT ('Dach', RESEED, 0) ", con);
                    SqlCommand cmd8 = new SqlCommand("DBCC CHECKIDENT ('BHNWorkInstructions', RESEED, 0) ", con);
                    SqlCommand cmd9 = new SqlCommand(@"Insert into Dach(IsDachOrder) values (" + dach + ")", con);
                    cmd1.ExecuteNonQuery();
                    cmd2.ExecuteNonQuery();
                    cmd3.ExecuteNonQuery();
                    cmd4.ExecuteNonQuery();
                    cmd5.ExecuteNonQuery();
                    cmd6.ExecuteNonQuery();
                    cmd7.ExecuteNonQuery();
                    cmd8.ExecuteNonQuery();
                    cmd9.ExecuteNonQuery();
                    con.Close();
                }

                // Insert new data into the database
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    int varID = 0;

                    foreach (DataRow row in jobs.Rows)
                    {
                        varID++;
                        using (SqlCommand cmd = new SqlCommand(@"INSERT INTO AthenaJobs 
                            (JobNumber, DueDate, PurchaseOrderNumber, PurchaseOrderLine, SalesOrderNumber, CustomerAccountCode, JobQuantity, AscmOrderId, EndCustomer, ActivationSystem, ProductType, ErpMaterialCode, IntegratorPartId, IntegratorID, ActivationType, PartNumberSku, RetailBarcode, RetailBarcodeType, Channel)
                            VALUES 
                            (@JobNumber, @DueDate, @PurchaseOrderNumber, @PurchaseOrderLine, @SalesOrderNumber, @CustomerAccountCode, @JobQuantity, @AscmOrderId, @EndCustomer, @ActivationSystem, @ProductType, @ErpMaterialCode, @IntegratorPartId, @IntegratorID, @ActivationType, @PartNumberSku, @RetailBarcode, @RetailBarcodeType, @Channel)", con))
                        {
                            //cmd.Parameters.AddWithValue("@Id", varID);
                            cmd.Parameters.AddWithValue("@JobNumber", row["JobNumber"].ToString());
                            cmd.Parameters.AddWithValue("@DueDate", row["DueDate"].ToString());
                            cmd.Parameters.AddWithValue("@PurchaseOrderNumber", row["PurchaseOrderNumber"].ToString());
                            cmd.Parameters.AddWithValue("@PurchaseOrderLine", row["PurchaseOrderLine"].ToString());
                            cmd.Parameters.AddWithValue("@SalesOrderNumber", row["SalesOrderNumber"].ToString());
                            cmd.Parameters.AddWithValue("@CustomerAccountCode", row["CustomerAccountCode"].ToString());
                            cmd.Parameters.AddWithValue("@JobQuantity", row["JobQuantity"].ToString());
                            cmd.Parameters.AddWithValue("@AscmOrderId", row["AscmOrderId"].ToString());
                            cmd.Parameters.AddWithValue("@EndCustomer", row["EndCustomer"].ToString());
                            cmd.Parameters.AddWithValue("@ActivationSystem", row["ActivationSystem"].ToString());
                            cmd.Parameters.AddWithValue("@ProductType", row["ProductType"].ToString());
                            cmd.Parameters.AddWithValue("@ErpMaterialCode", row["ErpMaterialCode"].ToString());
                            cmd.Parameters.AddWithValue("@IntegratorPartId", row["IntegratorPartId"].ToString());
                            cmd.Parameters.AddWithValue("@IntegratorID", row["IntegratorID"].ToString());
                            cmd.Parameters.AddWithValue("@ActivationType", row["ActivationType"].ToString());
                            cmd.Parameters.AddWithValue("@PartNumberSku", row["PartNumberSku"].ToString());
                            cmd.Parameters.AddWithValue("@RetailBarcode", row["RetailBarcode"].ToString());
                            cmd.Parameters.AddWithValue("@RetailBarcodeType", row["RetailBarcodeType"].ToString());
                            cmd.Parameters.AddWithValue("@Channel", row["Channel"].ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    con.Close();
                }

                // Fetch the newly inserted data to update the AthenaJobs list in the AthenaController
                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    var athenaJobs = context.AthenaJobs.ToList();
                    var athenaController = DependencyResolver.Current.GetService<AthenaController>();
                }

                return RedirectToAction("Index", "Athena", new {sliderValue});
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult ImportIntel(HttpPostedFileBase postedFile, FormCollection form)
        {
            if (postedFile == null || postedFile.ContentLength == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                return View("Index"); // Return to the view with the error message
            }

            try
            {
                // Save the uploaded file to a temporary path
                string tempFilePath = Server.MapPath("~/App_Data/Intel.csv");
                postedFile.SaveAs(tempFilePath);

                // Read the CSV file
                List<IntelModel> intelData = new List<IntelModel>();
                IntelModel intelRecord = new IntelModel();

                using (TextFieldParser parser = new TextFieldParser(tempFilePath))
                {
                    parser.TextFieldType = VBFileIO.FieldType.Delimited; // Use alias
                    parser.SetDelimiters(","); // Set the delimiter as comma
                    parser.HasFieldsEnclosedInQuotes = true; // Enable handling of quoted fields

                    // Optionally skip the header row
                    parser.ReadLine();

                    while (!parser.EndOfData)
                    {
                        // Read fields from the current line
                        string[] fields = parser.ReadFields();

                        intelRecord = new IntelModel
                        {
                            A = fields[0],
                            B = fields[1],
                            C = fields[2],
                            D = fields[3],
                            E = fields[4],
                            F = fields[5],
                            G = fields[6],
                            H = fields[7],
                            I = fields[8],
                            J = fields[9],
                            K = fields[10],
                            L = fields[11],
                            M = fields[12],
                            N = fields[13],
                            O = fields[14],
                            P = fields[15],
                            Q = fields[16],
                            R = fields[17],
                            S = fields[18],
                            T = fields[19],
                            U = fields[20],
                            V = fields[21],
                            W = fields[22],
                            X = fields[23],
                            Y = fields[24],
                            Z = fields[25],
                            AA = fields[26],
                            AB = fields[27],
                            AC = fields[28],
                            AD = fields[29],
                            AE = fields[30],
                            AF = fields[31],
                            AG = fields[32],
                            AH = fields[33],
                            AI = fields[34],
                            AJ = fields[35],
                            AK = fields[36],
                            AL = fields[37],
                            AM = fields[38],
                            AN = fields[39],
                            AO = fields[40],
                            AP = fields[41],
                            AQ = fields[42],
                            AR = fields[43],
                            AS = fields[44],
                            AT = fields[45],
                        };

                        intelData.Add(intelRecord);
                    }
                }

                return RedirectToAction("Index", "Athena", intelRecord);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult ImportBlackhawk(HttpPostedFileBase postedFile, FormCollection form)
        {
            if (postedFile == null || postedFile.ContentLength == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                return View("Index"); // Return to the view with the error message
            }

            try
            {
                // Save the uploaded file to a temporary path
                string tempFilePath = Server.MapPath("~/App_Data/Blackhawk.csv");
                postedFile.SaveAs(tempFilePath);

                // Read the CSV file
                List<BlackhawkModel> blackhawkData = new List<BlackhawkModel>();
                BlackhawkModel blackhawkRecord = new BlackhawkModel();

                using (SqlConnection con = new SqlConnection(_connectionString)) 
                {
                    using (TextFieldParser parser = new TextFieldParser(tempFilePath))
                    {
                        parser.TextFieldType = VBFileIO.FieldType.Delimited; // Use alias
                        parser.SetDelimiters(","); // Set the delimiter as comma
                        parser.HasFieldsEnclosedInQuotes = true; // Enable handling of quoted fields

                        // Optionally skip the header row
                        parser.ReadLine();

                        while (!parser.EndOfData)
                        {
                            // Read fields from the current line
                            string[] fields = parser.ReadFields();

                            blackhawkRecord = new BlackhawkModel
                            {
                                A = fields[0],
                                B = fields[1],
                                C = fields[2],
                                D = fields[3],
                                E = fields[4],
                                F = fields[5],
                                G = fields[6],
                                H = fields[7],
                                I = fields[8],
                                J = fields[9],
                                K = fields[10],
                                L = fields[11],
                                M = fields[12],
                                N = fields[13],
                                O = fields[14],
                                P = fields[15],
                                Q = fields[16],
                                R = fields[17],
                                S = fields[18],
                                T = fields[19],
                                U = fields[20],
                                V = fields[21],
                                W = fields[22],
                                X = fields[23],
                                Y = fields[24],
                                Z = fields[25],
                                AA = fields[26],
                                AB = fields[27],
                                AC = fields[28],
                                AD = fields[29],
                                AE = fields[30],
                                AF = fields[31],
                                AG = fields[32],
                                AH = fields[33],
                                AI = fields[34],
                                AJ = fields[35],
                                AK = fields[36],
                                AL = fields[37],
                                AM = fields[38],
                                AN = fields[39],
                                AO = fields[40],
                                AP = fields[41],
                                AQ = fields[42],
                            };

                            blackhawkData.Add(blackhawkRecord);

                            string pattern = @"PO-\w+";
                            string po = "";

                            Match match = Regex.Match(blackhawkRecord.K, pattern);
                            if (match.Success)
                            {
                                po = match.Value;
                            }

                            con.Open();
                            using (SqlCommand cmd = new SqlCommand(@"INSERT INTO BHNWorkInstructions (SKU, PO)VALUES (@SKU, @PO )", con))
                            {
                                //cmd.Parameters.AddWithValue("@Id", varID);
                                cmd.Parameters.AddWithValue("@SKU", blackhawkRecord.E.ToString());
                                cmd.Parameters.AddWithValue("@PO", po);

                                cmd.ExecuteNonQuery();
                            }
                            con.Close();

                        }
                    }
                }              

                return RedirectToAction("Index", "Athena");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        public ActionResult ImportDLC(HttpPostedFileBase postedFile)
        {
            postedFile.SaveAs(Server.MapPath("~/App_Data/temp.xls"));
            string filePath = Server.MapPath("~/App_Data/temp.xls");
            int numRows = 0;
            DLCModel dlcRecord = new DLCModel();

            using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    while (reader.Read())
                    {
                        numRows++;
                    }

                    var x = reader.AsDataSet();
                    DataTable d = x.Tables[0];
                    if (d.Rows[0][0].ToString().ToUpper() == "PROJECT ID")
                    {
                        numRows = d.Rows.Count - 1;
                    }

                    List<DLCModel> DLCData = new List<DLCModel>();
                    DLCModel DLCRecord = new DLCModel();


                    int fieldcount = reader.FieldCount;
                    int rowcount = reader.RowCount;
                    DataTable dt = new DataTable();
                    DataRow row;
                    DataTable dt_ = new DataTable();
                    try
                    {
                        dt_ = reader.AsDataSet().Tables[0];
                        for (int i = 0; i < dt_.Columns.Count; i++)
                        {
                            dt.Columns.Add(dt_.Rows[0][i].ToString());
                        }
                        int rowcounter = 0;
                        for (int row_ = 1; row_ < dt_.Rows.Count; row_++)
                        {
                            row = dt.NewRow();

                            for (int col = 0; col < dt_.Columns.Count; col++)
                            {
                                row[col] = dt_.Rows[row_][col].ToString();
                                rowcounter++;
                            }
                            dt.Rows.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["errorMessage"] = ex.ToString();
                        ModelState.AddModelError("File", "Unable to Upload file!");
                        return View();
                    }                                  

                    List<DLCModel> DLCList = new List<DLCModel>();
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string security = dt.Rows[i][8].ToString();
                        string productKey = dt.Rows[i][9].ToString();
                        string printedMaterial = dt.Rows[i][8].ToString();

                        if (printedMaterial.Contains("Printed Material"))
                        {
                            dlcRecord.ArtworkPartNumber = dt.Rows[i][1].ToString();
                        }

                        if (security.Contains("Security") && productKey.Contains("ProductKey"))
                        {
                            // Job Type
                            dlcRecord.JobType = "DLC";

                            // Description
                            if (dt.Rows[i][17].ToString() != "")
                            {
                                dlcRecord.Description = dt.Rows[i][2].ToString();
                            }

                            // ASSET ID 1
                            if (dt.Rows[i][17].ToString() != "")
                            {
                                // Asset ID 1
                                dlcRecord.AssetId1 = dt.Rows[i][17].ToString();
                            }
                            // Bom Comment 1
                            if (dt.Rows[i][7].ToString() != "")
                            {
                                string comment1 = dt.Rows[i][7].ToString().TrimStart().TrimEnd();
                                Regex expression1 = new Regex("\"([^\"]+)\"|\\*([^\\*]+)\\*");
                                Match match1 = expression1.Match(comment1);
                                string result1 = match1.Value.Replace('"', ' ').Trim();
                                result1 = result1.Replace("*", ""); // remove any remaining * symbol
                                dlcRecord.BOMcomment1 = result1;
                            }

                            // BOM COMMENT 2 / ASSET ID 2
                            if (dt.Rows.Count > i + 1)
                            {
                                // Asset ID 2
                                dlcRecord.AssetId2 = dt.Rows[i + 1][17].ToString();

                                // Bom Comment 2
                                string comment2 = dt.Rows[i + 1][7].ToString().TrimStart().TrimEnd();
                                Regex expression2 = new Regex("\"([^\"]+)\"|\\*([^\\*]+)\\*");// extracts the text between both double quotes and asterisks - "..." or *...*
                                Match match2 = expression2.Match(comment2);
                                string result2 = match2.Value.Replace('"', ' ').Trim();
                                result2 = result2.Replace("*", "");
                                dlcRecord.BOMcomment2 = result2;
                            }

                            // BOM COMMENT 3 / ASSET ID 3
                            if (dt.Rows.Count > i + 2)
                            {
                                // Asset ID 3
                                dlcRecord.AssetId3 = dt.Rows[i + 2][17].ToString();

                                // Bom Comment 3
                                string comment3 = dt.Rows[i + 2][7].ToString().TrimStart().TrimEnd();
                                Regex expression3 = new Regex("\"([^\"]+)\"|\\*([^\\*]+)\\*");
                                Match match3 = expression3.Match(comment3);
                                string result3 = match3.Value.Replace('"', ' ').Trim();
                                result3 = result3.Replace("*", "");
                                dlcRecord.BOMcomment3 = result3;
                            }

                            // BOM COMMENT 4 / ASSET ID 4
                            if (dt.Rows.Count > i + 3)
                            {
                                // Asset ID 4
                                dlcRecord.AssetId4 = dt.Rows[i + 3][17].ToString();

                                // Bom Comment 4
                                string comment4 = dt.Rows[i + 3][7].ToString().TrimStart().TrimEnd();
                                Regex expression4 = new Regex("\"([^\"]+)\"|\\*([^\\*]+)\\*");
                                Match match4 = expression4.Match(comment4);
                                string result4 = match4.Value.Replace('"', ' ').Trim();
                                result4 = result4.Replace("*", "");
                                dlcRecord.BOMcomment4 = result4;
                            }

                            // BOM COMMENT 5 / ASSET ID 5
                            if (dt.Rows.Count > i + 4)
                            {
                                // Asset ID 5
                                dlcRecord.AssetId5 = dt.Rows[i + 4][17].ToString();

                                // Bom Comment 5
                                string comment5 = dt.Rows[i + 4][7].ToString().TrimStart().TrimEnd();
                                Regex expression5 = new Regex("\"([^\"]+)\"|\\*([^\\*]+)\\*");
                                Match match5 = expression5.Match(comment5);
                                string result5 = match5.Value.Replace('"', ' ').Trim();
                                result5 = result5.Replace("*", "");
                                dlcRecord.BOMcomment5 = result5;
                            }

                            DLCList.Add(dlcRecord);
                        }
                    }

                }
                return RedirectToAction("Index", "Athena", dlcRecord);
            }
        }

        public class Order
        {
            public string Id { get; set; }
            public string[] Values { get; set; }

            public Order() { }

            public Order(string id, string[] values)
            {
                Id = id;
                Values = values;
            }
        }


        [HttpPost]
        public ActionResult ImportOrderAsasara(HttpPostedFileBase postedFile)
        {
            if (postedFile == null || postedFile.ContentLength == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No file uploaded.");
                ModelState.AddModelError("file", "Please select a file to upload.");
                return RedirectToAction("Index", "Asasara");
            }

            try
            {
                // Save the uploaded file to a temporary path
                string tempFilePath = Server.MapPath("~/App_Data/Temp.xls");
                postedFile.SaveAs(tempFilePath);

                // Load the XML document
                XDocument xmlDoc = XDocument.Load(tempFilePath);

                // Determine the structure and count of elements in the XML
                int numOfJobs = xmlDoc.Root.Elements().Count();
                int numOfFieldsInJob = xmlDoc.Root.Elements().ElementAt(0).Elements().ElementAt(0).Elements().Count();

                // Create an array to store job information
                Order[] jobsArray = new Order[numOfJobs];

                for (int i = 0; i < numOfJobs; i++)
                {
                    // Create and fill string array with values from XML file
                    string[] values = new string[numOfFieldsInJob];

                    for (int j = 0; j < numOfFieldsInJob; j++)
                    {
                        if (xmlDoc.Root.Elements().ElementAt(i).Elements().ElementAt(0).Elements().ElementAt(j).Elements().ElementAt(0).Value == "")
                        {
                            values[j] = "N/A";
                        }
                        else
                        {
                            values[j] = xmlDoc.Root.Elements().ElementAt(i).Elements().ElementAt(0).Elements().ElementAt(j).Elements().ElementAt(0).Value;
                        }
                    }

                    // Create new order and place it in array of orders
                    jobsArray[i] = new Order((i + 1).ToString(), values);
                }

                // Convert jobsArray to DataTable
                DataTable jobs = new DataTable("Jobs");
                jobs.Columns.Add("JobNumber");
                jobs.Columns.Add("SalesOrder");
                jobs.Columns.Add("BuildQty");
                jobs.Columns.Add("CustomerAccountCode");
                jobs.Columns.Add("PartNumber");
                jobs.Columns.Add("OCR");
                jobs.Columns.Add("OrderOsLink");
                jobs.Columns.Add("8");
                jobs.Columns.Add("9");
                jobs.Columns.Add("10");
                jobs.Columns.Add("11");
                jobs.Columns.Add("12");
                jobs.Columns.Add("13");
                jobs.Columns.Add("14");
                jobs.Columns.Add("15");
                jobs.Columns.Add("16");
                jobs.Columns.Add("17");
                jobs.Columns.Add("18");
                jobs.Columns.Add("19");
                jobs.Columns.Add("20");
                jobs.Columns.Add("21");
                
                foreach (var order in jobsArray)
                {
                    DataRow row = jobs.NewRow();
                    //row["Id"] = order.Id;
                    for (int i = 0; i < order.Values.Length; i++)
                    {
                        row[i + 1] = order.Values[i];
                    }
                    jobs.Rows.Add(row);
                }

                // Delete existing data from the database tables
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    SqlCommand cmd1 = new SqlCommand("DELETE FROM AsasaraJobs", con);
                    SqlCommand cmd2 = new SqlCommand("DELETE FROM ExportAsasara", con);
                    SqlCommand cmd3 = new SqlCommand("DELETE FROM Orders", con);
                    SqlCommand cmd4 = new SqlCommand("DBCC CHECKIDENT ('AsasaraJobs', RESEED, 0) ", con);
                    SqlCommand cmd5 = new SqlCommand("DBCC CHECKIDENT ('ExportAsasara', RESEED, 0) ", con);

                    cmd1.ExecuteNonQuery();
                    cmd2.ExecuteNonQuery();
                    cmd3.ExecuteNonQuery();
                    cmd4.ExecuteNonQuery();
                    cmd5.ExecuteNonQuery();
                    con.Close();
                }

                // Insert new data into the database
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    int varID = 0;

                    foreach (DataRow row in jobs.Rows)
                    {
                        varID++;
                        using (SqlCommand cmd = new SqlCommand(@"INSERT INTO Orders 
                            (ID, JobNumber, SalesOrder, CustomerAccountCode, BuildQty, PartNumber, OCR, OrderOsLink)
                            VALUES 
                            (@Id,  @JobNumber, @SalesOrder, @CustomerAccountCode, @BuildQty, @PartNumber, @OCR, @OrderOsLink)", con))
                        {
                            cmd.Parameters.AddWithValue("@Id", varID);
                            cmd.Parameters.AddWithValue("@JobNumber", row[1]);
                            cmd.Parameters.AddWithValue("@SalesOrder", row[5].ToString());
                            cmd.Parameters.AddWithValue("@CustomerAccountCode", row[6].ToString());
                            cmd.Parameters.AddWithValue("@BuildQty", row[7].ToString());
                            cmd.Parameters.AddWithValue("@PartNumber", row[16].ToString());
                            cmd.Parameters.AddWithValue("@OCR", row[19].ToString());
                            cmd.Parameters.AddWithValue("@OrderOsLink", row[16].ToString().Split('_').Last()); 

                            cmd.ExecuteNonQuery();
                        }
                    }
                    con.Close();
                }

                // Fetch the newly inserted data to update the AsasaraJobs list in the AsasaraController
                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    var asasaraJobs = context.AsasaraJobs.ToList();
                    var asasaraController = DependencyResolver.Current.GetService<AsasaraController>();
                }

                return RedirectToAction("Index", "Asasara");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult ImportWorkInstructionsAsasara(HttpPostedFileBase postedFile)
        {

            if (postedFile == null || postedFile.ContentLength == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                return View("Index"); // Return to the view with the error message
            }
            else
            {
                // Save CSV to WorkInstructions table in database
                string orderFileName;
                string orderFilePath;
                string connectStr = ConfigurationManager.ConnectionStrings["ConString"].ConnectionString;

                orderFileName = postedFile.FileName;
                orderFilePath = Server.MapPath("~/App_Data/AsasaraWorkInstructions.csv");
                postedFile.SaveAs(orderFilePath);

                // First Clear Out WorkInstructions table
                using (SqlConnection con1 = new SqlConnection(connectStr))
                {
                    con1.Open();

                    SqlCommand cmd1 = new SqlCommand("Delete from AsasaraWorkinstructions", con1);
                    cmd1.ExecuteNonQuery();
                    con1.Close();
                }

                // Then Read in CSV fields between Quotes via regex
                Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                StreamReader srMyStream = new StreamReader(orderFilePath);
                int val = 0;
                while (!srMyStream.EndOfStream)
                {
                    var s = srMyStream.ReadLine();
                    String[] Fields = CSVParser.Split(s);

                    for (int i = 0; i < Fields.Length; i++) // clean up the fields (remove " and leading spaces)
                    {
                        Fields[i] = Fields[i].TrimStart(' ', '"');
                        Fields[i] = Fields[i].TrimStart(' ', '"');
                        Fields[i] = Fields[i].TrimEnd('"');
                    }

                    List<string> list = new List<string>();
                    list = Fields.ToList();
                    if (list.Count == 45)
                    {
                        list.Add("");
                        list.Add("");
                        list.Add("");
                        list.Add("");
                        list.Add("");
                    }

                    val = val + 1;
                    string val0 = list[0].Trim();
                    string val1 = list[1].Trim();
                    string val2 = list[2].Trim();
                    string val3 = list[3].Trim();
                    string val4 = list[4].Trim();
                    string val5 = list[5].Trim();
                    string val6 = list[6].Trim();
                    string val7 = list[7].Trim();
                    string val8 = list[8].Trim();
                    string val9 = list[9].Trim();
                    string val10 = list[10].Trim();
                    string val11 = list[11].Trim();
                    string val12 = list[12].Trim();
                    string val13 = list[13].Trim();
                    string val14 = list[14].Trim();
                    string val15 = list[15].Trim();
                    string val16 = list[16].Trim();
                    string val17 = list[17].Trim();
                    string val18 = list[18].Trim();
                    string val19 = list[19].Trim();
                    string val20 = list[20].Trim();
                    string val21 = list[21].Trim();
                    string val22 = list[22].Trim();
                    string val23 = list[23].Trim();
                    string val24 = list[24].Trim();
                    string val25 = list[25].Trim();
                    string val26 = list[26].Trim();
                    string val27 = list[27].Trim();
                    string val28 = list[28].Trim();
                    string val29 = list[29].Trim();
                    string val30 = list[30].Trim();
                    string val31 = list[31].Trim();
                    string val32 = list[32].Trim();
                    string val33 = list[33].Trim();
                    string val34 = list[34].Trim();
                    string val35 = list[35].Trim();
                    string val36 = list[36].Trim();
                    string val37 = list[37].Trim();
                    string val38 = list[38].Trim();
                    string val39 = list[39].Trim();
                    string val40 = list[40].Trim();
                    string val41 = list[41].Trim();
                    string val42 = list[42].Trim();
                    string val43 = list[43].Trim();
                    string val44 = list[44].Trim();
                    string val45 = list[45].Trim();
                    string val46 = list[46].Trim();
                    string val47 = list[47].Trim();

                    using (SqlConnection con2 = new SqlConnection(connectStr))
                    {
                        SqlCommand cmd2 = new SqlCommand("INSERT INTO AsasaraWorkInstructions (" +
                         "ID, " +
                        "Campaign_ID__rName, " +
                        "Integrator_Project_ID__c, " +
                        "Printer_Project_ID__c, " +
                        "MMYY__c, " +
                        "Integrator_SHARE__c, " +
                        "Printer_SHARE__c, " +
                        "Identifier__c, " +
                        "Line_Description__c, " +
                        "Country__c, " +
                        "Integrator_Product_ID__c, " +
                        "In_DC_Quantity__c, " +
                        "In_DC_Date__c, " +
                        "Production_UPC__c, " +
                        "Form_Factor__c, " +
                        "Denomination__c, " +
                        "Partner_Encoding_Type__c, " +
                        "Internal_Activation__c, " +
                        "Production_Run_Group__c, " +
                        "IsUpcOnCard__c, " +
                        "IsReprint__c, " +
                        "Google_FAI_Quantity__c, " +
                        "Integrator_FAI_Quantity__c, " +
                        "Test_Quantity_Prod_Data_Proof__c, " +
                        "Test_Quantity_Sandbox_Dev__c, " +
                        "Production_Quantity__c, " +
                        "Google_Data_Production_Quantity__c, " +
                        "Ship_to_Location_Text__c, " +
                        "Ship_to_Location_Contact__c, " +
                        "Ship_to_Location_1_Test_Cards_Text__c, " +
                        "Ship_to_Location_1_Test_Cards_Contact__c, " +
                        "Ship_to_Location_2_Test_Cards_Text__c, " +
                        "Ship_to_Location_2_Test_Cards_Contact__c, " +
                        "Cards_per_Pack__c, " +
                        "Packs_per_Carton__c, " +
                        "Cards_per_Carton__c, " +
                        "Pack_EAN__c, " +
                        "Pallet_EAN__c, " +
                        "Case_EAN__c, " +
                        "Spec_Guide_Version__c, " +
                        "Outerbox_Caption__c, " +
                        "Logistics_Guide_Version__c, " +
                        "[NAN_Code__c-Shipping_PO_c-Product_Packing_Label_c], " +
                        "[BHN_Brand_Code_c-BHN_1st_Case_Quantity_c-PID_Number_c], " +
                        "[BHN_Pack_Description_c-Packing_UPC_c], " +
                        "[Packaging__c-Encoding_Identifier__c], " +
                        "Label_Spec__c, " +
                        "IsCardImportFileRequired__c, " +
                        "IsSendFirstBoxToBHN__c)"

                        + "VALUES('" + val + "','" + val0 + "', '" + val1 + "', '" + val2 + "', '" + val3 + "', '" + val4 + "', '" + val5 + "', '" + val6 + "', '" + val7 + "', '" + val8 + "', '" + val9 + "', '" + val10 + "', '" + val11 + "', '" + val12 + "', '" + val13 + "', '" + val14 + "', '" + val15 + "', '" + val16 + "', '" + val17 + "', '" + val18 + "', '" + val19 + "', '" + val20 + "', '" + val21 + "', '" + val22 + "', '" + val23 + "', '" + val24 + "', '" + val25 + "', '" + val26 + "', '" + val27 + "', '" + val28 + "', '" + val29 + "', '" + val30 + "', '" + val31 + "', '" + val32 + "', '" + val33 + "', '" + val34 + "', '" + val35 + "', '" + val36 + "', '" + val37 + "', '" + val38 + "', '" + val39 + "', '" + val40 + "', '" + val41 + "', '" + val42 + "', '" + val43 + "', '" + val44 + "', '" + val45 + "', '" + val46 + "', '" + val47 + "'); ", con2);

                        con2.Open();
                        if (val != 1) // do not import the headers
                        {
                            cmd2.ExecuteNonQuery();
                        }
                    }
                }
                srMyStream.Close();

                using (SqlConnection con3 = new SqlConnection(connectStr))
                {
                    using (SqlCommand cmd3 = new SqlCommand("AsasaraRemoveSplitShipLines", con3))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        con3.Open();
                        cmd3.ExecuteNonQuery();
                    }
                    con3.Close();
                }

                using (SqlConnection con = new SqlConnection(connectStr))
                {
                    using (SqlCommand cmd = new SqlCommand("AsasaraClearJobs", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                    con.Close();
                    using (SqlCommand cmd = new SqlCommand("AsasaraUpdateJobs", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }
            }
            //return RedirectToAction("Index", "Asasara", OSRecord);
            return RedirectToAction("Index", "Asasara");
           

        }

    }
}




































//jobs.Columns.Add("CampaignID");
//jobs.Columns.Add("ProjectID");
//jobs.Columns.Add("MMYY");
//jobs.Columns.Add("IntegratorShare");
//jobs.Columns.Add("PrinterShare"); 
//jobs.Columns.Add("Identifier");
//jobs.Columns.Add("LineDescription");
//jobs.Columns.Add("Country");
//jobs.Columns.Add("IntegratorProductID");
//jobs.Columns.Add("Quantity");
//jobs.Columns.Add("InDCDate");
//jobs.Columns.Add("ProductionUPC");
//jobs.Columns.Add("FormFactor");
//jobs.Columns.Add("Denomination");
//jobs.Columns.Add("PartnerEncodingType");
//jobs.Columns.Add("InternalActivation");
//jobs.Columns.Add("ProductionRunGroup");
//jobs.Columns.Add("IsUPCONCard");
//jobs.Columns.Add("IsReprint");
//jobs.Columns.Add("GoogleFAIQuantity");
//jobs.Columns.Add("IntegratorFAIQuantity");
//jobs.Columns.Add("TestQuantityProductionDataProof");
//jobs.Columns.Add("TestQuantitySandboxDev");
//jobs.Columns.Add("ShipToLocationText");
//jobs.Columns.Add("ShipToLocationContact");
//jobs.Columns.Add("ShipToLocation1TestCardsText");
//jobs.Columns.Add("ShipToLocation1TestCardsContact");
//jobs.Columns.Add("ShipToLocation2TestCardsText");
//jobs.Columns.Add("ShipToLocation2TestCardsContact");
//jobs.Columns.Add("CardsPerPack");
//jobs.Columns.Add("PacksPerCarton");
//jobs.Columns.Add("CardsPerCarton");
//jobs.Columns.Add("PackEAN");
//jobs.Columns.Add("PalletEAN");
//jobs.Columns.Add("CaseEAN");
//jobs.Columns.Add("SpecGuideVersion");
//jobs.Columns.Add("LogisticsGuideVersion");
//jobs.Columns.Add("NAN-ShippingPO-ProductPackingLabel");
//jobs.Columns.Add("BHNBrandCode-BHN1stCaseQuantity-PIDNumber");
//jobs.Columns.Add("BHNPackDescription-PackingUPC");
//jobs.Columns.Add("Packaging-EncodingIdentifier");
//jobs.Columns.Add("LabelSpec");
//jobs.Columns.Add("OuterBoxCaption");
//jobs.Columns.Add("IsCardImportFileRequired");
//jobs.Columns.Add("IsSendFirstBoxToBHN");