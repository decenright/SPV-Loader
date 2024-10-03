using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using Microsoft.Ajax.Utilities;
using SPV_Loader;
using SPV_Loader.Models;
using System.Configuration;

namespace SPV_Loader.Controllers
{
    public class AsasaraController : Controller
    {
        private static List<AsasaraJob> asasaraJobs = new List<AsasaraJob>();
        private static List<Order> asasaraOrders = new List<Order>();
        private static List<Asasara_Order_WorkInstructions> asasaraCombinedDetailsList = new List<Asasara_Order_WorkInstructions>();
        private static AsasaraJob asasaraJob = new AsasaraJob();
        private static Asasara_Order_WorkInstructions asasaraCombinedDetails = new Asasara_Order_WorkInstructions();
        private static ExportAsasara exportAsasara = new ExportAsasara();

        // Track the current index
        private static int currentIndex = -1;

        // GET: Asasara
        public ActionResult Index()
        {
            @TempData["LoaderName"] = "ASASARA";

            if (currentIndex == -1)
            {
                TempData["allItemsProcessed"] = false;
                clearJobs();
                currentIndex++;
            }
            var viewModel = new AsasaraViewModel();
            ViewBag.CurrentIndex = currentIndex;

            using (SpvLoaderEntities context = new SpvLoaderEntities())
            {
                asasaraJobs = context.AsasaraJobs.ToList();
                asasaraOrders = context.Orders.ToList();
                asasaraCombinedDetailsList = context.Asasara_Order_WorkInstructions.ToList();
            }

            if (currentIndex != asasaraOrders.Count()) // if all jobs have not been processed
            {

                viewModel = new AsasaraViewModel
                {
                    AsasaraDetails = asasaraJobs.Count > 0 ? asasaraJobs[currentIndex] : new AsasaraJob(), // gets the current job by the current index
                    ExportAsasara = new ExportAsasara(),
                    AsasaraList = asasaraJobs,
                    OrdersList = asasaraOrders,
                };
            }
            else // display empty view
            {
                viewModel = new AsasaraViewModel
                {
                    AsasaraDetails = asasaraJob,
                    ExportAsasara = exportAsasara,
                    AsasaraList = asasaraJobs,
                    OrdersList = asasaraOrders,
                };
            }

            if (viewModel.AsasaraDetails.Integrator_Share == "BHN")
            {
                bool isNewActivation = CheckNewActivation(viewModel);
                if (isNewActivation)
                {
                    @TempData["NewActivation"] = "WARNING - new ActivationType";
                    viewModel.AsasaraDetails.Partner_Encoding_Type = "WARNING - new ActivationType";
                }
                else
                {
                    @TempData["NewActivation"] = "";
                }

                var asasaraProcessBHNController = new AsasaraProcessBHNController(); // Instantiate the AsasaraProcessBHNController and get the extra details for the current job
                var processedJob = asasaraProcessBHNController.ProcessJob(viewModel);
                viewModel.ExportAsasara = processedJob;
            }

            if (viewModel.AsasaraDetails.Integrator_Share == "ePay")
            {
                bool isNewActivation = CheckNewActivation(viewModel);
                if (isNewActivation)
                {
                    @TempData["NewActivation"] = "WARNING - new ActivationType";
                    viewModel.AsasaraDetails.Partner_Encoding_Type = "WARNING - new ActivationType";
                }
                else
                {
                    @TempData["NewActivation"] = "";
                }

                var asasaraProcessEPYController = new AsasaraProcessEPYController(); // get the extra details for the current job
                var processedJob = asasaraProcessEPYController.ProcessJob(viewModel);
                viewModel.ExportAsasara = processedJob;
            }

            if (viewModel.AsasaraDetails.Integrator_Share == "Incomm")
            {
                bool isNewActivation = CheckNewActivation(viewModel);
                if (isNewActivation)
                {
                    @TempData["NewActivation"] = "WARNING - new ActivationType";
                    viewModel.AsasaraDetails.Partner_Encoding_Type = "WARNING - new ActivationType";
                }
                else
                {
                    @TempData["NewActivation"] = "";
                }

                var asasaraProcessINCController = new AsasaraProcessINCController(); // get the extra details for the current job
                var processedJob = asasaraProcessINCController.ProcessJob(viewModel);
                viewModel.ExportAsasara = processedJob;
            }

            return View(viewModel);
        }

        public ActionResult New()
        {
            TempData["allItemsProcessed"] = false;
            clearJobs();
            currentIndex = -1;
            return RedirectToAction("Index");
        }

        public static void clearJobs()
        {
            string _connectionString = "Data Source=CM-APP-SVR\\SQLEXPRESS;Initial Catalog=SpvLoader;Integrated Security=true";
            // Delete existing data from the database tables
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlCommand cmd1 = new SqlCommand("DELETE FROM AsasaraJobs", con);
                SqlCommand cmd2 = new SqlCommand("DBCC CHECKIDENT ('AsasaraJobs', RESEED, 0) ", con);
                SqlCommand cmd3 = new SqlCommand("DELETE FROM Orders", con);
                SqlCommand cmd4 = new SqlCommand("DBCC CHECKIDENT ('Orders', RESEED, 0) ", con);
                cmd1.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                cmd3.ExecuteNonQuery();
                con.Close();
            }
        }

        [HttpPost]
        public ActionResult SaveAndNext(AsasaraViewModel model)
        {
            try
            {
                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    // Save the current job if the currentIndex is valid
                    if (currentIndex < asasaraJobs.Count)
                    {
                        var currentJob = asasaraJobs[currentIndex];

                        model.ExportAsasara = new ExportAsasara
                        {
                            customerJobNumber = currentJob.PartNumber,
                            jobID = currentJob.JobNumber.ToString(),
                            orderID = currentJob.SalesOrder.ToString(),                            
                            jobQty = currentJob.Quantity,
                            integratorName = currentJob.Integrator_Share,

                            activationType = model.ExportAsasara.activationType,
                            regionID = model.ExportAsasara.regionID,
                            pptQty = model.ExportAsasara.pptQty,
                            denomination = model.ExportAsasara.denomination,
                            currency = model.ExportAsasara.currency,
                            retailBarcode = model.ExportAsasara.retailBarcode,
                            integratorID = model.ExportAsasara.integratorID,
                            productDescription = model.ExportAsasara.productDescription,
                            maskID = model.ExportAsasara.maskID,
                            dummyRecords = model.ExportAsasara.dummyRecords,
                            dummyRecordsCount = model.ExportAsasara.dummyRecordsCount,
                            dummyRecordsEvery = model.ExportAsasara.dummyRecordsEvery,
                            packQty = model.ExportAsasara.packQty,
                            caseQty = model.ExportAsasara.caseQty,
                            palletQty = model.ExportAsasara.palletQty,
                            expiryDate = model.ExportAsasara.expiryDate,
                            whiteCardTestQty = model.ExportAsasara.whiteCardTestQty,
                            retailBarcodeType = model.ExportAsasara.retailBarcodeType,
                            barcodeStyleType = model.ExportAsasara.barcodeStyleType,
                            alternativePartNumber = model.ExportAsasara.alternativePartNumber,
                            country = model.ExportAsasara.country,
                            eanBundle = model.ExportAsasara.eanBundle,
                            eanBox = model.ExportAsasara.eanBox,
                            DODHumanFontTypeID = model.ExportAsasara.DODHumanFontTypeID,
                            partnerTextBox = model.ExportAsasara.partnerTextBox,
                            partnerCode = model.ExportAsasara.partnerCode,
                            brand = model.ExportAsasara.brand,
                            VASBoxDescription = model.ExportAsasara.VASBoxDescription,
                            palletTypeID = model.ExportAsasara.palletTypeID,
                            comments = model.ExportAsasara.comments,
                            recipientAddress1 = model.ExportAsasara.recipientAddress1,
                            recipientAddress2 = model.ExportAsasara.recipientAddress2,
                            recipientAddress3 = model.ExportAsasara.recipientAddress3,
                            recipientAddress4 = model.ExportAsasara.recipientAddress4,
                            recipientAddress5 = model.ExportAsasara.recipientAddress5,
                            recipientAddress6 = model.ExportAsasara.recipientAddress6,
                            codeFormat = model.ExportAsasara.codeFormat,
                            vasType = model.ExportAsasara.vasType,
                            FAIQty = model.ExportAsasara.FAIQty,
                            OCR = model.ExportAsasara.OCR,
                            VASPackDescription = model.ExportAsasara.VASPackDescription,
                            labelStyle = model.ExportAsasara.labelStyle,
                            activationMode = model.ExportAsasara.activationMode,
                            pinFile = model.ExportAsasara.pinFile,
                            WCTPinFile = model.ExportAsasara.WCTPinFile,
                            eanPallet = model.ExportAsasara.eanPallet,
                            BHNPONumber = model.ExportAsasara.BHNPONumber,
                        };

                        context.ExportAsasaras.Add(model.ExportAsasara);
                        context.SaveChanges();
                    }

                    // Move to the next job
                    currentIndex++;

                    if (currentIndex < asasaraJobs.Count)
                    {
                        var nextJob = asasaraJobs[currentIndex];

                        model.AsasaraDetails = new AsasaraJob
                        {
                            JobNumber = nextJob.JobNumber,

                            CustomerAccountCode = nextJob.CustomerAccountCode,

                        };
                    }
                    else
                    {
                        // All items processed
                        TempData["allItemsProcessed"] = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle it as needed
                TempData["errorMessage"] = ex.ToString();
            }

            return RedirectToAction("Index");
        }

        public ActionResult Download()
        {
            try
            {
                // Initialize the DataTable with the specified table name
                DataTable AsasaraXMLImport = new DataTable() { TableName = "AsasaraImport" };

                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    var exportData = context.ExportAsasaras.ToList();

                    // Add columns to the DataTable, excluding the "Id" column
                    foreach (var prop in typeof(ExportAsasara).GetProperties())
                    {
                        if (prop.Name != "Id") // Exclude the primary key "Id"
                        {
                            AsasaraXMLImport.Columns.Add(prop.Name, typeof(string)); // Ensure all columns are of type string to avoid issues with DBNull
                        }
                    }

                    // Add rows to the DataTable
                    foreach (var record in exportData)
                    {
                        DataRow row = AsasaraXMLImport.NewRow();
                        foreach (var prop in typeof(ExportAsasara).GetProperties())
                        {
                            if (prop.Name != "Id") // Exclude the primary key "Id"
                            {
                                var value = prop.GetValue(record);
                                row[prop.Name] = value ?? DBNull.Value;
                            }
                        }
                        AsasaraXMLImport.Rows.Add(row);
                    }
                }

                // Create an XDocument to structure the desired XML output
                XDocument xmlDoc = new XDocument(new XElement("DocumentElement",
                    AsasaraXMLImport.AsEnumerable().Select(row => new XElement("AsasaraImport",
                        AsasaraXMLImport.Columns.Cast<DataColumn>().Select(col =>
                            new XElement(col.ColumnName, row[col] == DBNull.Value ? string.Empty : row[col].ToString()))))));

                // Define file path and name
                string filename = "AsasaraImport-" + DateTime.Now.ToString("dd-MM-yyyy") + ".xml";
                string filePath = Server.MapPath("~/App_Data/AsasaraImport.xml");

                // Save the XDocument to the specified file path
                xmlDoc.Save(filePath);

                // Return the XML file as a response
                Response.ContentType = "application/xml";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + filename);
                Response.TransmitFile(filePath);
                Response.End();
            }
            catch (Exception ex)
            {
                TempData["errorMessage"] = ex.ToString();
            }

            return new EmptyResult();
        }

        private static bool CheckNewActivation(AsasaraViewModel asasaraViewModel)
        {
            var job = asasaraViewModel.AsasaraDetails;
            string connectStr = ConfigurationManager.ConnectionStrings["ConString"].ConnectionString;
            DataTable ActivationTypeLookup = new DataTable();
            string queryActivationType = @"select * from AsasaraActivationType";
            SqlConnection conActType = new SqlConnection(connectStr);
            SqlCommand cmdActType = new SqlCommand(queryActivationType, conActType);
            conActType.Open();
            SqlDataAdapter daActType = new SqlDataAdapter(cmdActType);
            daActType.Fill(ActivationTypeLookup);
            conActType.Close();
            daActType.Dispose();

            string activationType = "";
            if (asasaraViewModel.AsasaraDetails.Integrator_Share == "ePay")
            {
                activationType = job.Partner_Encoding_Type.ToString();
                if (job.Partner_Encoding_Type.ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
                {
                    activationType = "Hybrid GS1128/Mag 19 digits";
                }
            }
            if (asasaraViewModel.AsasaraDetails.Integrator_Share == "BHN")
            {
                activationType = ActivationType(asasaraViewModel);
            }

            if (asasaraViewModel.AsasaraDetails.Integrator_Share == "Incomm")
            {
                activationType = job.Partner_Encoding_Type.ToString();
                if (activationType.ToUpper() == "TIBIDONO CODE128")
                {
                    activationType = "16Serial128";
                }
            }


            bool isNewActivation = true;  // Assume new activation by default

            for (int j = 0; j < ActivationTypeLookup.Rows.Count; j++)
            {
                string lookupValue = ActivationTypeLookup.Rows[j][1].ToString().Replace(System.Environment.NewLine, "");

                if (activationType.ToUpper() == lookupValue.ToUpper())
                {
                    isNewActivation = false;  // Match found, set to false
                    break;  // No need to continue the loop, exit early
                }
            }
            // If no match is found, isNewActivation will remain true
            return isNewActivation;
        }

        private static string ActivationType(AsasaraViewModel asasaraViewModel)
        {
            string activationType = "";
            bool workInstructionsLoaded = false; 
            using (SpvLoaderEntities context = new SpvLoaderEntities())
            {
                var workInstructions = context.AsasaraWorkInstructions.Count();
                if (workInstructions == 0)
                {
                    workInstructionsLoaded = false;
                }
                else
                {
                    workInstructionsLoaded = true;
                }
            }
            if (workInstructionsLoaded == true)
            {
                string activation = asasaraViewModel.AsasaraDetails.Internal_Activation.ToString();
                string country = asasaraViewModel.AsasaraDetails.Country.ToString();
                string integrator = asasaraViewModel.AsasaraDetails.Integrator_Share;

                if (activation.ToUpper() == "BARCODE+MAGSTRIPE" || activation.ToUpper() == "BARCODE + MAGSTRIPE")
                {
                    activation = "Hybrid";
                }

                if (country == "ES" || country == "AT" || country == "FR" || country == "DE" || country == "CH" || country == "PL" || country == "ZA")
                {
                    activationType = integrator + " " + activation + " C128";
                }
                else if (country == "UK" || country == "NI" || country == "GB" || country == "IE" || country == "ROI" || country == "BE" || country == "NL" || country == "SE" || country == "DK" || country == "NO" || country == "FI")
                {
                    activationType = integrator + " " + activation + " UCC/EAN128";
                }
                else if (country == "IT")
                {
                    activationType = "BHN GS1-128 34/37 digits";
                }
                else
                {
                    activationType = "Country Invalid";
                }
            }

            return activationType;
        }
    }
}







