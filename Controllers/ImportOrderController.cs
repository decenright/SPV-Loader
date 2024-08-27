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
                    //athenaController.SetAthenaJobs(athenaJobs);
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

            //string jobNo = form["AthenaDetails.JobNumber"];

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

                        string A = "BusinessModel = " + intelRecord.P + " " + "\n";
                        string B = "ScratchON/OFF = " + intelRecord.H + " " + "\n";
                        string C = "PartNumber = " + intelRecord.G + " " + "\n";
                        string D = "TopLevelSku = " + intelRecord.E + " " + "\n";
                        double result;
                        double.TryParse(intelRecord.AF, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                        string E = "CartonRetailCode = " + result.ToString() + "\n";
                        string F = "QrCodeRequired = " + intelRecord.AT + " ";
                        string jobComment = A + B + C + D + E + F;
                        intelRecord.JobComment = jobComment;

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

            //string jobNo = form["AthenaDetails.JobNumber"];

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
                                dlcRecord.BOMcomment1 = result1;
                            }

                            // BOM COMMENT 2 / ASSET ID 2
                            if (dt.Rows.Count > i + 1)
                            {
                                // Asset ID 2
                                dlcRecord.AssetId2 = dt.Rows[i + 1][17].ToString();

                                // Bom Comment 2
                                string comment2 = dt.Rows[i + 1][7].ToString().TrimStart().TrimEnd();
                                Regex expression2 = new Regex("\"([^\"]+)\"|\\*([^\\*]+)\\*"); // extracts the text between both double quotes and asterisks "..." or *...*
                                Match match2 = expression2.Match(comment2);
                                string result2 = match2.Value.Replace('"', ' ').Trim();
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
    }
}







//public ActionResult ImportIntel(HttpPostedFileBase postedFile)
//{
//    if (postedFile == null || postedFile.ContentLength == 0)
//    {
//        //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No file uploaded.");
//        ModelState.AddModelError("file", "Please select a file to upload.");

//        try
//        {
//            // Save the uploaded file to a temporary path
//            string tempFilePath = Server.MapPath("~/App_Data/intel.csv");
//            postedFile.SaveAs(tempFilePath);
//        }
//        catch(Exception ex)
//        {
//            return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
//        }



//        return RedirectToAction("Index", "Athena");
//    }

//    return RedirectToAction("Index", "Athena");
//}

//// Make sure the correct BOM is imported
//string partNumber = PartNumberTextBox.Text;
//partNumber = partNumber.Remove(partNumber.Length - 5);

//if (partNumber.Contains("_"))
//{
//    partNumber = partNumber.Replace("_", "");
//}

//if (partNumber != ArtworkPartNumberTextBox.Text)
//{
//    lblErrorMessage.Text = "ArtworkPartNumber does not match - Please load the correct BOM";
//    lblErrorMessage.Visible = true;
//}