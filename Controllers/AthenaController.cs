using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using SPV_Loader;
using SPV_Loader.Models;

namespace SPV_Loader.Controllers
{
    public class AthenaController : Controller
    {
        private static List<AthenaJob> athenaJobs = new List<AthenaJob>();
        private static AthenaJob athenaJob = new AthenaJob();
        private static ExportAthena exportAthena = new ExportAthena();

        // Track the current index
        private static int currentIndex = -1;

        public ActionResult Index(IntelModel intelRecord, BlackhawkModel blackhawkRecord, DLCModel dlcRecord)
        {
            @TempData["LoaderName"] = "ATHENA";

            if (currentIndex == -1)
            {
                TempData["allItemsProcessed"] = false;
                clearJobs();
                currentIndex++;
            }
            var viewModel = new AthenaViewModel();
            bool dach = false;

            using (SpvLoaderEntities context = new SpvLoaderEntities())
            {
                athenaJobs = context.AthenaJobs.ToList();

                var isDach = context.Daches.FirstOrDefault(); // check if DACH order
                if (isDach != null) 
                {
                    if (isDach.IsDachOrder == true)
                    {
                        dach = true;
                    }
                }
            }
            
            if(currentIndex != athenaJobs.Count()) // if all jobs have not been processed
            {
                viewModel = new AthenaViewModel
                {
                    AthenaDetails = athenaJobs.Count > 0 ? athenaJobs[currentIndex] : new AthenaJob(),
                    ExportAthena = new ExportAthena(),
                    AthenaList = athenaJobs,
                    IsDach = dach,
                    DLCModel = dlcRecord,
                };

                @TempData["IntegratorId"] = viewModel.AthenaDetails.IntegratorID;
                @TempData["Channel"] = viewModel.AthenaDetails.Channel;
                @TempData["POline"] = viewModel.AthenaDetails.PurchaseOrderLine;

                //INTEL
                string jobComment = intelRecord.JobComment;
                if (jobComment != null)
                {
                    viewModel.ExportAthena.JobComments = jobComment;
                }
                if (intelRecord.E != null) // Top Level SKU
                {
                    viewModel.ExportAthena.AlternativePartNumber = intelRecord.E;
                }

                // DLC
                if (dlcRecord.AssetId1 != null) 
                {
                    viewModel.ExportAthena.PKPN1 = dlcRecord.AssetId1;
                }
                if (dlcRecord.BOMcomment1 != null)
                {
                    viewModel.ExportAthena.BOMComment1 = dlcRecord.BOMcomment1;
                }
                if (dlcRecord.AssetId2 != null)
                {
                    viewModel.ExportAthena.PKPN2 = dlcRecord.AssetId2;
                }
                if (dlcRecord.BOMcomment2 != null)
                {
                    viewModel.ExportAthena.BOMComment2 = dlcRecord.BOMcomment2;
                }
                if (dlcRecord.AssetId3 != null)
                {
                    viewModel.ExportAthena.PKPN3 = dlcRecord.AssetId3;
                }
                if (dlcRecord.BOMcomment3 != null)
                {
                    viewModel.ExportAthena.BOMComment3 = dlcRecord.BOMcomment3;
                }
                if (dlcRecord.AssetId4 != null)
                {
                    viewModel.ExportAthena.PKPN4 = dlcRecord.AssetId4;
                }
                if (dlcRecord.BOMcomment4 != null)
                {
                    viewModel.ExportAthena.BOMComment4 = dlcRecord.BOMcomment4;
                }
                if (dlcRecord.AssetId5 != null)
                {
                    viewModel.ExportAthena.PKPN5 = dlcRecord.AssetId5;
                }
                if (dlcRecord.BOMcomment5 != null)
                {
                    viewModel.ExportAthena.BOMComment5 = dlcRecord.BOMcomment5;
                }
                viewModel.ExportAthena.JobTypeLVId = dlcRecord.JobType;
                viewModel.ExportAthena.Description = dlcRecord.Description;
            }
            else // display empty view
            {
                viewModel = new AthenaViewModel
                {
                    AthenaDetails = athenaJob,
                    ExportAthena = exportAthena,
                    AthenaList = athenaJobs,
                    IsDach = false
                };
            }

            viewModel.DLCModel = dlcRecord;
            var athenaProcessController = new AthenaProcessController(); // Instantiate the AthenaProcessController and get the extra details for the current job
            var processedJob = athenaProcessController.ProcessJob(viewModel);
            viewModel.ExportAthena = processedJob;

            return View(viewModel);
        }

        public static void clearJobs()
        {
            string _connectionString = "Data Source=CM-APP-SVR\\SQLEXPRESS;Initial Catalog=SpvLoader;Integrated Security=true";
            // Delete existing data from the database tables
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                SqlCommand cmd1 = new SqlCommand("DELETE FROM AthenaJobs", con);
                SqlCommand cmd2 = new SqlCommand("DBCC CHECKIDENT ('AthenaJobs', RESEED, 0) ", con);
                SqlCommand cmd3 = new SqlCommand("DELETE FROM Dach", con);
                SqlCommand cmd4 = new SqlCommand("DBCC CHECKIDENT ('Dach', RESEED, 0) ", con);
                SqlCommand cmd5 = new SqlCommand(@"Insert into Dach(IsDachOrder) values (" + "0" + ")", con);
                cmd1.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                cmd3.ExecuteNonQuery();

                con.Close();
            }
        }

        public ActionResult New()
        {
            TempData["allItemsProcessed"] = false;
            clearJobs();
            currentIndex = -1;
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Process(AthenaViewModel model)
        {
            if (model.ExportAthena.PartNumberSku != null && model.AthenaDetails.PartNumberSku != null)
            {
                if (model.AthenaDetails.PartNumberSku != model.ExportAthena.PartNumberSku)
                {
                    return View("ErrorDLC");
                }
            }

            try
            {
                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    // Save the current job if the currentIndex is valid
                    if (currentIndex < athenaJobs.Count)
                    {
                        var currentJob = athenaJobs[currentIndex];

                        model.ExportAthena = new ExportAthena
                        {
                            Id = currentJob.Id,
                            JobId = currentJob.JobNumber,
                            OrderId = currentJob.SalesOrderNumber,
                            PurchaseOrderNumber = currentJob.PurchaseOrderNumber,
                            PurchaseOrderLine = currentJob.PurchaseOrderLine,
                            CustomerAccountCode = currentJob.CustomerAccountCode,
                            JobQty = currentJob.JobQuantity.ToString(),
                            ASCMOrderID = currentJob.AscmOrderId,
                            EndCustomer = currentJob.EndCustomer,
                            ActivationSystem = currentJob.ActivationSystem,
                            ProductType = currentJob.ProductType,
                            ErpMaterialCode = currentJob.ErpMaterialCode,
                            FAIStart = model.ExportAthena.FAIStart,
                            FAIEnd = model.ExportAthena.FAIEnd,
                            ContractTypeLVId = model.ExportAthena.ContractTypeLVId,
                            PartNumberSku = model.ExportAthena.PartNumberSku,

                            IncommProductDescription = model.ExportAthena.IncommProductDescription,
                            PackagingGTIN = model.ExportAthena.PackagingGTIN,
                            AlternativePartNumber = model.ExportAthena.AlternativePartNumber,
                            JobComments = model.ExportAthena.JobComments,
                            Denomination = model.DachCountry,
                            DenominationCurrency = model.DachDescription,
                            BHNPONumber = model.ExportAthena.BHNPONumber,
                        };

                        context.ExportAthenas.Add(model.ExportAthena);
                        context.SaveChanges();
                    }

                    // Move to the next job
                    currentIndex++;

                    if (currentIndex < athenaJobs.Count)
                    {
                        var nextJob = athenaJobs[currentIndex];

                        model.AthenaDetails = new AthenaJob
                        {
                            Id = nextJob.Id,
                            JobNumber = nextJob.JobNumber,
                            DueDate = nextJob.DueDate,
                            SalesOrderNumber = nextJob.SalesOrderNumber,
                            PurchaseOrderNumber = nextJob.PurchaseOrderNumber,
                            PurchaseOrderLine = nextJob.PurchaseOrderLine,
                            CustomerAccountCode = nextJob.CustomerAccountCode,
                            JobQuantity = nextJob.JobQuantity,
                            AscmOrderId = nextJob.AscmOrderId,
                            EndCustomer = nextJob.EndCustomer,
                            ActivationSystem = nextJob.ActivationSystem,
                            ProductType = nextJob.ProductType,
                            ErpMaterialCode = nextJob.ErpMaterialCode,
                            IntegratorPartID = nextJob.IntegratorPartID,
                            IntegratorID = nextJob.IntegratorID,
                            ActivationType = nextJob.ActivationType,
                            PartNumberSku = nextJob.PartNumberSku,
                            RetailBarcode = nextJob.RetailBarcode,
                            RetailBarcodeType = nextJob.RetailBarcodeType,
                            Channel = nextJob.Channel,
                        };
                        // Retain IsDach, DachCountry, DachDescription
                        model.IsDach = model.IsDach;
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
                DataTable AthenaXMLImport = new DataTable() { TableName = "AthenaImport" };

                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    var exportData = context.ExportAthenas.ToList();

                    // Add columns to the DataTable, excluding the "Id" column
                    foreach (var prop in typeof(ExportAthena).GetProperties())
                    {
                        if (prop.Name != "Id") // Exclude the primary key "Id"
                        {
                            AthenaXMLImport.Columns.Add(prop.Name, typeof(string)); // Ensure all columns are of type string to avoid issues with DBNull
                        }
                    }

                    // Add rows to the DataTable
                    foreach (var record in exportData)
                    {
                        DataRow row = AthenaXMLImport.NewRow();
                        foreach (var prop in typeof(ExportAthena).GetProperties())
                        {
                            if (prop.Name != "Id") // Exclude the primary key "Id"
                            {
                                var value = prop.GetValue(record);
                                row[prop.Name] = value ?? DBNull.Value;
                            }
                        }
                        AthenaXMLImport.Rows.Add(row);
                    }
                }

                // Create an XDocument to structure the desired XML output
                XDocument xmlDoc = new XDocument(new XElement("DocumentElement",
                    AthenaXMLImport.AsEnumerable().Select(row => new XElement("AthenaImport",
                        AthenaXMLImport.Columns.Cast<DataColumn>().Select(col =>
                            new XElement(col.ColumnName, row[col] == DBNull.Value ? string.Empty : row[col].ToString()))))));

                // Define file path and name
                string filename = "AthenaImport-" + DateTime.Now.ToString("dd-MM-yyyy") + ".xml";
                string filePath = Server.MapPath("~/App_Data/AthenaImport.xml");

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
    }
}












//public ActionResult Download()
//{
//    // Initialize the DataTable with the specified table name
//    DataTable AthenaXMLImport = new DataTable() { TableName = "AthenaImport" };

//    // Retrieve data using Entity Framework context
//    try
//    {
//        using (SpvLoaderEntities context = new SpvLoaderEntities())
//        {
//            var exportData = context.ExportAthenas.ToList();

//            foreach (var prop in typeof(ExportAthena).GetProperties())
//            {
//                AthenaXMLImport.Columns.Add(prop.Name, prop.PropertyType);
//            }

//            foreach (var record in exportData)
//            {
//                DataRow row = AthenaXMLImport.NewRow();
//                foreach (var prop in typeof(ExportAthena).GetProperties())
//                {
//                    //row[prop.Name] = prop.GetValue(record) ?? DBNull.Value;
//                    row[prop.Name] = prop.GetValue(record);
//                }
//                AthenaXMLImport.Rows.Add(row);
//            }
//        }

//        AthenaXMLImport.Columns.RemoveAt(0);  // remove primary key 

//        string filename = "AthenaImport-" + DateTime.Now.ToString("dd-MM-yyyy") + ".xml";
//        string filePath = Server.MapPath("~/App_Data/AthenaImport.xml");
//        AthenaXMLImport.WriteXml(filePath);

//        Response.ContentType = "application/xml";
//        Response.AppendHeader("Content-Disposition", "attachment; filename=" + filename);
//        Response.TransmitFile(filePath);
//        Response.End();
//    }
//    catch (Exception ex)
//    {
//        TempData["errorMessage"] = ex.ToString();
//    }

//    return new EmptyResult();
//}










//public List<AthenaJob> GetItems()
//{
//    List<AthenaJob> jobs = new List<AthenaJob>();
//    try
//    {
//        using (SpvLoaderEntities context = new SpvLoaderEntities())
//        {
//            jobs = context.AthenaJobs.ToList();
//        }
//    }
//    catch (Exception ex)
//    {
//        TempData["errorMessage"] = ex.ToString();
//    }
//    return jobs;
//}

//public ExportAthena GetExport(int id)
//{
//    ExportAthena export = null;
//    try
//    {
//        using (SpvLoaderEntities context = new SpvLoaderEntities())
//        {
//            export = context.ExportAthenas.FirstOrDefault(d => d.Id == id);
//        }
//    }
//    catch (Exception ex)
//    {
//        // Log the error or handle it as needed
//        TempData["errorMessage"] = ex.ToString();
//    }
//    return export;
//}





//public ActionResult Process(AthenaViewModel model)
//{
//    if(currentIndex > 0)
//    {
//        using (SpvLoaderEntities context = new SpvLoaderEntities())
//        {
//            // Save the processed job back to the database
//            model.ExportAthena = new ExportAthena
//            {
//                Id = model.AthenaDetails.Id,
//                JobId = model.AthenaDetails.JobNumber,
//                OrderId = model.AthenaDetails.SalesOrderNumber,
//                PurchaseOrderNumber = model.AthenaDetails.PurchaseOrderNumber,
//                PurchaseOrderLine = model.AthenaDetails.PurchaseOrderLine,
//                CustomerAccountCode = model.AthenaDetails.CustomerAccountCode,
//                JobQty = model.AthenaDetails.JobQuantity.ToString(),
//                ASCMOrderID = model.AthenaDetails.AscmOrderId,
//                EndCustomer = model.AthenaDetails.EndCustomer,
//                ActivationSystem = model.AthenaDetails.ActivationSystem,
//                ProductType = model.AthenaDetails.ProductType,
//                ErpMaterialCode = model.AthenaDetails.ErpMaterialCode,

//                FAIStart = model.ExportAthena.FAIStart,
//                FAIEnd = model.ExportAthena.FAIEnd,

//                IncommProductDescription = model.ExportAthena.IncommProductDescription,
//                PackagingGTIN = model.ExportAthena.PackagingGTIN,
//                AlternativePartNumber = model.ExportAthena.AlternativePartNumber,
//            };

//            context.ExportAthenas.Add(model.ExportAthena);
//            context.SaveChanges();
//        }
//    }
//    //var model = new AthenaViewModel();

//    // get the next job and display it in the view
//    if (ModelState.IsValid)
//    {
//        try
//        {
//            using (SpvLoaderEntities context = new SpvLoaderEntities())
//            {
//                var job = context.AthenaJobs.FirstOrDefault(x => x.Id == currentIndex +1);

//                // Instantiate the AthenaProcessController
//                var athenaProcessController = new AthenaProcessController();

//                // Process the job using AthenaProcessController
//                var processedJob = athenaProcessController.ProcessJob(job);

//                if (processedJob == null)
//                {
//                    throw new Exception("Processed job is null");
//                }

//                model.ExportAthena = new ExportAthena
//                {
//                    Id = processedJob.Id,
//                    JobId = processedJob.JobId,
//                    OrderId = processedJob.OrderId,
//                    PurchaseOrderNumber = processedJob.PurchaseOrderNumber,
//                    PurchaseOrderLine = processedJob.PurchaseOrderLine,
//                    CustomerAccountCode = processedJob.CustomerAccountCode,
//                    JobQty = processedJob.JobQty,
//                    ASCMOrderID = processedJob.ASCMOrderID,
//                    EndCustomer = processedJob.EndCustomer,
//                    ActivationSystem = processedJob.ActivationSystem,
//                    ProductType = processedJob.ProductType,
//                    ErpMaterialCode = processedJob.ErpMaterialCode,

//                    FAIStart = processedJob.FAIStart,
//                    FAIEnd = processedJob.FAIEnd,
//                };

//                model.AthenaDetails = new AthenaJob
//                {
//                    Id = job.Id,
//                    JobNumber = job.JobNumber,
//                    DueDate = job.DueDate,
//                    SalesOrderNumber = job.SalesOrderNumber,
//                    PurchaseOrderNumber = job.PurchaseOrderNumber,
//                    PurchaseOrderLine = job.PurchaseOrderLine,
//                    CustomerAccountCode = job.CustomerAccountCode,
//                    JobQuantity = job.JobQuantity, 
//                    AscmOrderId = job.AscmOrderId,
//                    EndCustomer = job.EndCustomer,
//                    ActivationSystem = job.ActivationSystem,
//                    ProductType = job.ProductType,
//                    ErpMaterialCode = job.ErpMaterialCode,
//                    IntegratorPartID = job.IntegratorPartID,
//                    IntegratorID = job.IntegratorID,
//                    ActivationType = job.ActivationType,
//                    PartNumberSku = job.PartNumberSku,
//                    RetailBarcode = job.RetailBarcode,
//                    RetailBarcodeType = job.RetailBarcodeType,
//                    Channel = job.Channel,
//                };

//                model.AthenaList = context.AthenaJobs.ToList();
//            }
//        }
//        catch (Exception ex)
//        {
//            // Log the error or handle it as needed
//            TempData["errorMessage"] = ex.ToString();
//        }
//    }

//    // Move to the next item in the list
//    currentIndex++;
//    return View("Index", model);
//}











//public AthenaJob GetDetails(int id)
//{
//    AthenaJob detail = null;
//    try
//    {
//        using (SpvLoaderEntities context = new SpvLoaderEntities())
//        {
//            detail = context.AthenaJobs.FirstOrDefault(d => d.Id == id);
//        }
//    }
//    catch (Exception ex)
//    {
//        // Log the error or handle it as needed
//        TempData["errorMessage"] = ex.ToString();
//    }
//    return detail;
//}




//model.AthenaDetails = new AthenaJob
//{
//    Id = currentJob.Id,
//    JobNumber = currentJob.JobNumber,
//    DueDate = currentJob.DueDate,
//    PurchaseOrderNumber = currentJob.PurchaseOrderNumber,
//    PurchaseOrderLine = currentJob.PurchaseOrderLine,
//    SalesOrderNumber = currentJob.SalesOrderNumber,
//    CustomerAccountCode = currentJob.CustomerAccountCode,
//    JobQuantity = currentJob.JobQuantity,
//    AscmOrderId = currentJob.AscmOrderId,
//    EndCustomer = currentJob.EndCustomer,
//    ActivationSystem = currentJob.ActivationSystem,
//    ProductType = currentJob.ProductType,
//    ErpMaterialCode = currentJob.ErpMaterialCode,
//    IntegratorPartID = currentJob.IntegratorPartID,
//    IntegratorID = currentJob.IntegratorID,
//    ActivationType = currentJob.ActivationType,
//    PartNumberSku = currentJob.PartNumberSku,
//    RetailBarcode = currentJob.RetailBarcode,
//    RetailBarcodeType = currentJob.RetailBarcodeType,
//    Channel = currentJob.Channel
//};






//if (job != null)
//{
//    // Update the job details with the values from the model
//    job.JobNumber = model.AthenaDetails.JobNumber;
//    job.DueDate = model.AthenaDetails.DueDate;
//    job.PurchaseOrderNumber = model.AthenaDetails.PurchaseOrderNumber;
//    job.PurchaseOrderLine = model.AthenaDetails.PurchaseOrderLine;
//    job.SalesOrderNumber = model.AthenaDetails.SalesOrderNumber;
//    job.CustomerAccountCode = model.AthenaDetails.CustomerAccountCode;
//    job.JobQuantity = model.AthenaDetails.JobQuantity;
//    job.AscmOrderId = model.AthenaDetails.AscmOrderId;
//    job.EndCustomer = model.AthenaDetails.EndCustomer;
//    job.ActivationSystem = model.AthenaDetails.ActivationSystem;
//    job.ProductType = model.AthenaDetails.ProductType;
//    job.ErpMaterialCode = model.AthenaDetails.ErpMaterialCode;
//    job.IntegratorPartID = model.AthenaDetails.IntegratorPartID;
//    job.IntegratorID = model.AthenaDetails.IntegratorID;
//    job.ActivationType = model.AthenaDetails.ActivationType;
//    job.PartNumberSku = model.AthenaDetails.PartNumberSku;
//    job.RetailBarcode = model.AthenaDetails.RetailBarcode;
//    job.RetailBarcodeType = model.r;
//    job.Channel = model.AthenaDetails.Channel;

//    context.SaveChanges();
//}
//else
//{
//    return HttpNotFound("Job not found");
//}









//public ActionResult Index()
//{
//    var model = new AthenaViewModel
//    {
//        AthenaList = athenaJobs,
//        AthenaDetails = athenaJobs.Count > 0 ? athenaJobs[currentIndex] : new AthenaJob()
//    };

//    return View(model);
//}

//[HttpPost]
//[ValidateAntiForgeryToken]
//public ActionResult Save(AthenaViewModel model)
//{
//    if (ModelState.IsValid)
//    {
//        var job = athenaJobs[currentIndex];
//        var exportAthena = new ExportAthena();

//        // Process the job using AthenaProcessController
//        var athenaProcessController = new AthenaProcessController();
//        exportAthena = athenaProcessController.ProcessJob(job);

//        model.AthenaDetails = job;
//        model.ExportAthena = exportAthena;
//        model.FAIStart = exportAthena.FAIStart;
//        model.FAIEnd = exportAthena.FAIEnd;

//        currentIndex++;
//        if (currentIndex >= athenaJobs.Count)
//        {
//            ViewBag.AllItemsProcessed = true;
//        }
//    }

//    return View("Index", model);
//}












//protected void SaveForExport(ExportAthena job)
//{
//    if (job == null)
//    {
//        throw new ArgumentNullException(nameof(job), "ExportAthena object cannot be null");
//    }

//    using (var context = new SpvLoaderEntities())
//    {
//        var exportAthena = new ExportAthena
//        {
//            JobId = job.JobId,
//            OrderId = job.OrderId,
//            PurchaseOrderNumber = job.PurchaseOrderNumber,
//            PurchaseOrderLine = job.PurchaseOrderLine,
//            CustomerAccountCode = job.CustomerAccountCode,
//            JobQty = job.JobQty.ToString(),
//            ASCMOrderID = job.ASCMOrderID,
//            EndCustomer = job.EndCustomer,
//            ActivationSystem = job.ActivationSystem,
//            ProductType = job.ProductType,
//            ErpMaterialCode = job.ErpMaterialCode,
//            FAIStart = job.FAIStart,
//            FAIEnd = job.FAIEnd,
//            ContractTypeLVId = job.ContractTypeLVId,
//            PartNumberSku = job.PartNumberSku,
//            JobComments = job.JobComments,
//            JobTypeLVId = job.JobTypeLVId,
//            SpecificationLVId = job.SpecificationLVId,
//            UPC = job.UPC,
//            ArtworkPartNumber = job.ArtworkPartNumber,
//            PackQty = job.PackQty,
//            BoxQty = job.BoxQty,
//            PalletQty = job.PalletQty,
//            Description = job.Description,
//            IncommRetailer = job.IncommRetailer,
//            IncommProductDescription = job.IncommProductDescription,
//            Denomination = job.Denomination,
//            DenominationCurrency = job.DenominationCurrency,
//            AlternativePartNumber = job.ArtworkPartNumber,
//            PackagingGTIN = job.PackagingGTIN,
//            BHNPONumber = job.BHNPONumber,
//            MSRequestNumber1 = job.MSRequestNumber1,
//            BOMComment1 = job.BOMComment1,
//            PKPN1 = job.PKPN1,
//            MSRequestNumber2 = job.MSRequestNumber2,
//            BOMComment2 = job.BOMComment2,
//            PKPN2 = job.PKPN2,
//            MSRequestNumber3 = job.MSRequestNumber3,
//            BOMComment3 = job.BOMComment3,
//            PKPN3 = job.PKPN3,
//            MSRequestNumber4 = job.MSRequestNumber4,
//            BOMComment4 = job.BOMComment4,
//            PKPN4 = job.PKPN4,
//            MSRequestNumber5 = job.MSRequestNumber5,
//            BOMComment5 = job.BOMComment5,
//            PKPN5 = job.PKPN5,
//        };

//        context.ExportAthenas.Add(exportAthena);
//        context.SaveChanges();
//    }
//}










//public ActionResult Index()
//{
//    if (athenaJobs == null || !athenaJobs.Any())
//    {
//        athenaJobs = GetItems(); // Fetch the items
//    }

//    // Create an empty view model
//    var viewModel = new AthenaViewModel
//    {
//        AthenaDetails = new AthenaJob(),
//        ExportAthena = new ExportAthena(),
//        AthenaList = athenaJobs // Assuming AthenaList is part of the view model
//    };

//    ViewBag.AllItemsProcessed = !athenaJobs.Any();

//    return View(viewModel);
//}


//[HttpPost]
//public ActionResult Save(AthenaViewModel model)
//{
//    if (ModelState.IsValid)
//    {
//        try
//        {
//            if (athenaJobs == null)
//            {
//                athenaJobs = GetItems(); // Fetch the items
//            }

//            if (currentIndex < athenaJobs.Count)
//            {
//                var job = athenaJobs[currentIndex];

//                // Update the job details with the values from the model
//                job.JobNumber = model.AthenaDetails.JobNumber;
//                job.DueDate = model.AthenaDetails.DueDate;
//                job.PurchaseOrderNumber = model.AthenaDetails.PurchaseOrderNumber;
//                job.PurchaseOrderLine = model.AthenaDetails.PurchaseOrderLine;
//                job.SalesOrderNumber = model.AthenaDetails.SalesOrderNumber;
//                job.CustomerAccountCode = model.AthenaDetails.CustomerAccountCode;
//                job.JobQuantity = model.AthenaDetails.JobQuantity;
//                job.AscmOrderId = model.AthenaDetails.AscmOrderId;
//                job.EndCustomer = model.AthenaDetails.EndCustomer;
//                job.ActivationSystem = model.AthenaDetails.ActivationSystem;
//                job.ProductType = model.AthenaDetails.ProductType;
//                job.ErpMaterialCode = model.AthenaDetails.ErpMaterialCode;
//                job.IntegratorPartID = model.AthenaDetails.IntegratorPartID;
//                job.IntegratorID = model.AthenaDetails.IntegratorID;
//                job.ActivationType = model.AthenaDetails.ActivationType;
//                job.PartNumberSku = model.AthenaDetails.PartNumberSku;
//                job.RetailBarcode = model.AthenaDetails.RetailBarcode;
//                job.RetailBarcodeType = model.AthenaDetails.RetailBarcodeType;
//                job.Channel = model.AthenaDetails.Channel;

//                var processedJob = _athenaProcessController.ProcessJob(job);

//                // Save the processed job back to the database
//                using (var context = new SpvLoaderEntities())
//                {
//                    context.ExportAthenas.Add(processedJob);
//                    context.SaveChanges();
//                }

//                SaveForExport(processedJob);

//                // Update the view model with derived values
//                model.ExportAthena.FAIStart = processedJob.FAIStart;
//                model.ExportAthena.FAIEnd = processedJob.FAIEnd;

//                currentIndex++;
//            }
//        }
//        catch (Exception ex)
//        {
//            // Log the error or handle it as needed
//            TempData["errorMessage"] = ex.ToString();
//        }
//    }

//    if (currentIndex >= athenaJobs.Count)
//    {
//        currentIndex = 0; // Reset index if it exceeds the list
//        athenaJobs = new List<AthenaJob>(); // Clear the list
//        ViewBag.AllItemsProcessed = true;
//    }
//    else
//    {
//        ViewBag.AllItemsProcessed = false;
//    }

//    return View("Index", model);
//}






























//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Data;
//using System.Linq;
//using System.Web.Mvc;
//using SPV_Loader.Models;
//using SPV_Loader;
//using System.Web.UI.WebControls;

//namespace SPV_Loader.Controllers
//{
//    public class AthenaController : Controller
//    {
//        private readonly SpvLoaderEntities _context;
//        private readonly AthenaProcessController _athenaProcessController;

//        // Track the current index
//        private static int currentIndex = 0;
//        private static List<AthenaJob> athenaJobs = null;

//        public ActionResult Index()
//        {
//            if (athenaJobs == null)
//            {
//                athenaJobs = new List<AthenaJob>(); // Initialize as empty list
//            }

//            AthenaJob details = null;
//            if (currentIndex < athenaJobs.Count)
//            {
//                details = athenaJobs[currentIndex]; // Get the details object for the current index
//            }

//            // If details is null, instantiate it
//            if (details == null)
//            {
//                details = new AthenaJob();
//            }

//            var viewModel = new AthenaViewModel
//            {
//                AthenaList = athenaJobs,
//                AthenaDetails = details,
//                ExportAthena = new ExportAthena() // Ensure ExportAthena is also instantiated
//            };

//            if (currentIndex >= athenaJobs.Count && athenaJobs.Any())
//            {
//                currentIndex = 0; // Reset index if it exceeds the list
//                athenaJobs = new List<AthenaJob>(); // Clear the list
//                ViewBag.AllItemsProcessed = true;
//            }
//            else
//            {
//                ViewBag.AllItemsProcessed = false;
//            }

//            return View(viewModel);
//        }

//        public List<AthenaJob> GetItems()
//        {
//            List<AthenaJob> jobs = new List<AthenaJob>();
//            try
//            {
//                using (SpvLoaderEntities context = new SpvLoaderEntities())
//                {
//                    jobs = context.AthenaJobs.ToList();
//                }
//            }
//            catch (Exception ex)
//            {
//                // Log the error or handle it as needed
//                TempData["errorMessage"] = ex.ToString();
//            }
//            return jobs;
//        }

//        public AthenaJob GetDetails(int id)
//        {
//            AthenaJob detail = null;
//            try
//            {
//                using (SpvLoaderEntities context = new SpvLoaderEntities())
//                {
//                    detail = context.AthenaJobs.FirstOrDefault(d => d.Id == id);
//                }
//            }
//            catch (Exception ex)
//            {
//                // Log the error or handle it as needed
//                TempData["errorMessage"] = ex.ToString();
//            }
//            return detail;
//        }

//        [HttpPost]
//        public ActionResult Save(AthenaViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    using (SpvLoaderEntities context = new SpvLoaderEntities())
//                    {
//                        var job = context.AthenaJobs.FirstOrDefault(j => j.Id == model.AthenaDetails.Id);

//                        if (job != null)
//                        {
//                            // Update the job details with the values from the model
//                            job.JobNumber = model.AthenaDetails.JobNumber;
//                            job.DueDate = model.AthenaDetails.DueDate;
//                            job.PurchaseOrderNumber = model.AthenaDetails.PurchaseOrderNumber;
//                            job.PurchaseOrderLine = model.AthenaDetails.PurchaseOrderLine;
//                            job.SalesOrderNumber = model.AthenaDetails.SalesOrderNumber;
//                            job.CustomerAccountCode = model.AthenaDetails.CustomerAccountCode;
//                            job.JobQuantity = model.AthenaDetails.JobQuantity;
//                            job.AscmOrderId = model.AthenaDetails.AscmOrderId;
//                            job.EndCustomer = model.AthenaDetails.EndCustomer;
//                            job.ActivationSystem = model.AthenaDetails.ActivationSystem;
//                            job.ProductType = model.AthenaDetails.ProductType;
//                            job.ErpMaterialCode = model.AthenaDetails.ErpMaterialCode;
//                            job.IntegratorPartID = model.AthenaDetails.IntegratorPartID;
//                            job.IntegratorID = model.AthenaDetails.IntegratorID;
//                            job.ActivationType = model.AthenaDetails.ActivationType;
//                            job.PartNumberSku = model.AthenaDetails.PartNumberSku;
//                            job.RetailBarcode = model.AthenaDetails.RetailBarcode;
//                            job.RetailBarcodeType = model.AthenaDetails.RetailBarcodeType;
//                            job.Channel = model.AthenaDetails.Channel;

//                            // Save changes to the database if necessary
//                            context.SaveChanges();
//                        }

//                        if (job == null)
//                        {
//                            return HttpNotFound();
//                        }

//                        // Instantiate the AthenaProcessController
//                        var athenaProcessController = new AthenaProcessController();

//                        // Process the job using AthenaProcessController
//                        var processedJob = athenaProcessController.ProcessJob(job);

//                        // Save the processed job back to the database
//                        context.ExportAthenas.Add(processedJob); 
//                        context.SaveChanges();

//                        SaveForExport(processedJob);

//                        // Update the view model with derived values
//                        model.FAIStart = processedJob.FAIStart;
//                        model.FAIEnd = processedJob.FAIEnd;

//                        // Also update ExportAthena in the view model
//                        model.ExportAthena = processedJob;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    // Log the error or handle it as needed
//                    TempData["errorMessage"] = ex.ToString();
//                }
//            }

//            // Move to the next item in the list
//            currentIndex++;
//            return RedirectToAction("Index");
//        }

//        // Method to set the athenaJobs list
//        public void SetAthenaJobs(List<AthenaJob> jobs)
//        {
//            athenaJobs = jobs;
//            currentIndex = 0; // Reset the index
//        }

//        protected void SaveForExport(ExportAthena job)
//        {
//            using (var context = new SpvLoaderEntities())
//            {
//                var exportAthena = new ExportAthena
//                {
//                    JobId = job.JobId,
//                    OrderId = job.OrderId,
//                    PurchaseOrderNumber = job.PurchaseOrderNumber,
//                    PurchaseOrderLine = job.PurchaseOrderLine,
//                    CustomerAccountCode = job.CustomerAccountCode,
//                    JobQty = job.JobQty.ToString(),
//                    ASCMOrderID = job.ASCMOrderID,
//                    EndCustomer = job.EndCustomer,
//                    ActivationSystem = job.ActivationSystem,
//                    ProductType = job.ProductType,
//                    ErpMaterialCode = job.ErpMaterialCode,
//                    FAIStart = job.FAIStart, 
//                    FAIEnd = job.FAIEnd, 
//                    ContractTypeLVId = job.ContractTypeLVId, 
//                    PartNumberSku = job.PartNumberSku,
//                    JobComments = job.JobComments,
//                    JobTypeLVId = job.JobTypeLVId, 
//                    SpecificationLVId = job.SpecificationLVId, 
//                    UPC = job.UPC, 
//                    ArtworkPartNumber = job.ArtworkPartNumber, 
//                    PackQty = job.PackQty, 
//                    BoxQty = job.BoxQty, 
//                    PalletQty = job.PalletQty, 
//                    Description = job.Description, 
//                    IncommRetailer = job.IncommRetailer, 
//                    IncommProductDescription = job.IncommProductDescription, 
//                    Denomination = job.Denomination, 
//                    DenominationCurrency = job.DenominationCurrency, 
//                    AlternativePartNumber = job.ArtworkPartNumber, 
//                    PackagingGTIN = job.PackagingGTIN, 
//                    BHNPONumber = job.BHNPONumber, 
//                    MSRequestNumber1 = job.MSRequestNumber1, 
//                    BOMComment1 = job.BOMComment1, 
//                    PKPN1 = job.PKPN1, 
//                    MSRequestNumber2 = job.MSRequestNumber2, 
//                    BOMComment2 = job.BOMComment2, 
//                    PKPN2 = job.PKPN2, 
//                    MSRequestNumber3 = job.MSRequestNumber3, 
//                    BOMComment3 = job.BOMComment3, 
//                    PKPN3 = job.PKPN3,
//                    MSRequestNumber4 = job.MSRequestNumber4,
//                    BOMComment4 = job.BOMComment4, 
//                    PKPN4 = job.PKPN4, 
//                    MSRequestNumber5 = job.MSRequestNumber5, 
//                    BOMComment5 = job.BOMComment5, 
//                    PKPN5 = job.PKPN5, 
//                };

//                context.ExportAthenas.Add(exportAthena);
//                context.SaveChanges();
//            }
//        }

//        public ActionResult Download()
//        {
//            // Initialize the DataTable with the specified table name
//            DataTable AthenaXMLImport = new DataTable() { TableName = "AthenaImport" };

//            // Retrieve data using Entity Framework context
//            try
//            {
//                using (SpvLoaderEntities context = new SpvLoaderEntities())
//                {
//                    // Fetch data from the ExportAthena table
//                    var exportData = context.ExportAthenas.ToList();

//                    // Add columns to the DataTable based on ExportAthena properties
//                    foreach (var prop in typeof(ExportAthena).GetProperties())
//                    {
//                        AthenaXMLImport.Columns.Add(prop.Name, prop.PropertyType);
//                    }

//                    // Add rows to the DataTable
//                    foreach (var record in exportData)
//                    {
//                        DataRow row = AthenaXMLImport.NewRow();
//                        foreach (var prop in typeof(ExportAthena).GetProperties())
//                        {
//                            row[prop.Name] = prop.GetValue(record) ?? DBNull.Value;
//                        }
//                        AthenaXMLImport.Rows.Add(row);
//                    }
//                }

//                // Define the filename with the current date
//                string filename = "AthenaImport-" + DateTime.Now.ToString("dd-MM-yyyy") + ".xml";

//                // Write the DataTable to an XML file in the server's App_Data directory
//                string filePath = Server.MapPath("~/App_Data/AthenaImport.xml");
//                AthenaXMLImport.WriteXml(filePath);

//                // Set the response content type to XML and add a header for file download
//                Response.ContentType = "application/xml";
//                Response.AppendHeader("Content-Disposition", "attachment; filename=" + filename);
//                Response.TransmitFile(filePath);
//                Response.End();
//            }
//            catch (Exception ex)
//            {
//                // Handle exceptions (logging, showing error messages, etc.)
//                TempData["errorMessage"] = ex.ToString();
//            }

//            return new EmptyResult();
//        }


//    }
//}

































//protected void SaveForExport(AthenaJob job)
//{
//    // if TopLevelSKUtextbox is visible then flag alternative source for <IncommProductDescription> & <AlternativePartNumber>
//    // string switchTag = "";
//    //if (TopLevelSKUTextBox.Visible == true)
//    //{
//    //    switchTag = "McAffee";
//    //}
//    //else
//    //{
//    //    switchTag = "";
//    //}

//    string E1 = job.JobNumber;
//    string E2 = job.SalesOrderNumber;
//    string E3 = job.PurchaseOrderNumber;
//    string E4 = job.PurchaseOrderLine; //lblPOLineNo.Text;
//    string E5 = job.CustomerAccountCode; //CustomerAccountCodeTextBox.Text;
//    string E6 = job.JobQuantity.ToString(); //JobQuantityTextBox.Text;
//    string E7 = job.AscmOrderId; //AscmOrderIDTextBox.Text;
//    string E8 = job.EndCustomer; //EndCustomerTextBox.Text;
//    string E9 = job.ActivationSystem;  //ActivationSystemTextBox.Text;
//    string E10 = job.ProductType;  //ProductTypeTextBox.Text;
//    string E11 = job.ErpMaterialCode;  //ErpMaterialCodeTextBox.Text;
//    string E12 = ""; //FaiStartTextBox.Text;
//    string E13 = "";  //FaiEndTextBox.Text;
//    string E14 = "";  //ContractTypeLvTextBox.Text;
//    string E15 = job.PartNumberSku; //PartNumberTextBox.Text;
//    string E16 = ""; //JobCommentsTextBox.Text;
//    string E17 = "";  //JobTypeTextBox.Text;
//    string E18 = "";  //SpecificationTextBox.Text;
//    string E19 = "";  //UpcTextBox.Text;
//    string E20 = "";  //ArtworkPartNumberTextBox.Text;
//    string E21 = "";  //PackQtyTextBox.Text;
//    string E22 = "";  //BoxQtyTextBox.Text;
//    string E23 = "";  //PalletQtyTextBox.Text;
//    string E24 = "";  //DescriptionTextBox.Text;
//    string E25 = ""; // IncommRetailerTextBox.Tex
//    string E26 = "";  //WPNDescriptionTextBox.Text;
//    string E27 = "";  //DachDescriptionTextBox.Text; //DenominationTextBox.Text;
//    string E28 = "";  //DachCountryTextBox.Text;  //DenominationCurrencyTextBox.Text;
//    string E29 = "";  //SOWNoTextBox.Text;
//    string E30 = "";  //WPNTextBox.Text;
//    string E31 = "";  //BHNPONumberTextBox.Text; // BHN PO Number
//    string E32 = "";  //MsRequestNo1TextBox.Text;
//    string E33 = "";  //BomComment1TextBox.Text;
//    string E34 = "";  //Pkpn1TextBox.Text;
//    string E35 = "";  //MsRequestNo2TextBox.Text;
//    string E36 = "";  //BomComment2TextBox.Text;
//    string E37 = "";  //Pkpn2TextBox.Text;
//    string E38 = "";  //MsRequestNo3TextBox.Text;
//    string E39 = "";  //BomComment3TextBox.Text;
//    string E40 = "";  //Pkpn3TextBox.Text;
//    string E41 = "";  //MsRequestNo4TextBox.Text;
//    string E42 = "";  //BomComment4TextBox.Text;
//    string E43 = "";  //Pkpn4TextBox.Text;
//    string E44 = "";  //MsRequestNo5TextBox.Text;
//    string E45 = "";  //BomComment5TextBox.Text;
//    string E46 = "";  //Pkpn5TextBox.Text;

//    //if (switchTag == "McAffee")
//    //{
//    //    E26 = ProductNameTextBox.Text;
//    //    E29 = TopLevelSKUTextBox.Text;
//    //}

//    SqlConnection con = new SqlConnection(_connectionString);
//    SqlCommand cmd;
//    cmd = new SqlCommand(@"INSERT INTO ExportAthena(JobId, OrderId, PurchaseOrderNumber, PurchaseOrderLine, CustomerAccountCode, JobQty, ASCMOrderID, EndCustomer, ActivationSystem, ProductType, ErpMaterialCode, FAIStart, FAIEnd, ContractTypeLVId, PartNumberSku, JobComments, JobTypeLVId, SpecificationLVId, UPC, ArtworkPartNumber, PackQty, BoxQty, PalletQty, Description, IncommRetailer, IncommProductDescription, Denomination, DenominationCurrency, AlternativePartNumber, PackagingGTIN, BHNPONumber, MSRequestNumber1, BOMComment1, PKPN1, MSRequestNumber2, BOMComment2, PKPN2, MSRequestNumber3, BOMComment3, PKPN3, MSRequestNumber4, BOMComment4, PKPN4, MSRequestNumber5, BOMComment5, PKPN5) 
//    VALUES('" + E1 + "','" + E2 + "','" + E3 + "','" + E4 + "','" + E5 + "','" + E6 + "','" + E7 + "','" + E8 + "','" + E9 + "','" + E10 + "','" + E11 + "','" + E12 + "','" + E13 + "','" + E14 + "','" + E15 + "','" + E16 + "','" + E17 + "','" + E18 + "','" + E19 + "','" + E20 + "','" + E21 + "','" + E22 + "','" + E23 + "','" + E24 + "','" + E25 + "','" + E26 + "','" + E27 + "','" + E28 + "','" + E29 + "','" + E30 + "','" + E31 + "','" + E32 + "','" + E33 + "','" + E34 + "','" + E35 + "','" + E36 + "','" + E37 + "','" + E38 + "','" + E39 + "','" + E40 + "','" + E41 + "','" + E42 + "','" + E43 + "','" + E44 + "','" + E45 + "','" + E46 + "')", con);
//    con.Open();
//    cmd.ExecuteNonQuery();
//    con.Close();
//}

// Download action for the processed list
//[HttpPost]
//public ActionResult Download()
//{
//    DataTable AthenaXMLImport = new DataTable() { TableName = "AthenaImport" };
//    SqlConnection con = new SqlConnection(_connectionString);
//    SqlCommand cmd = new SqlCommand(@"Select * from ExportAthena", con);
//    con.Open();
//    SqlDataAdapter da = new SqlDataAdapter(cmd);
//    da.Fill(AthenaXMLImport);
//    cmd.ExecuteNonQuery();
//    con.Close();

//    string filename = "AthenaImport " + "-" + DateTime.Now.ToString(format: "dd/MM/yyyy") + ".xml";
//    try
//    {
//        AthenaXMLImport.WriteXml(Server.MapPath("~//App_Data//AthenaImport.xml"));
//    }
//    catch (Exception ex)
//    {
//    }

//    Response.ContentType = "application/xml";
//    Response.AppendHeader("Content-Disposition", "attachment; filename=" + filename);
//    Response.TransmitFile(Server.MapPath("~//App_Data//AthenaImport.xml"));
//    Response.End();

//    return new EmptyResult();
//}











//public ActionResult Index()
//{
//    var model = new AthenaViewModel();

//    using (SpvLoaderEntities context = new SpvLoaderEntities())
//    {
//        // Get the list of jobs
//        var jobs = context.AthenaJobs.ToList();

//        // Initialize the AthenaList with all jobs
//        model.AthenaList = jobs;

//        var job = context.AthenaJobs.FirstOrDefault();
//        model.AthenaDetails = job;

//        // Check if all items have been processed
//        if (currentIndex >= jobs.Count)
//        {
//            ViewBag.AllItemsProcessed = true;
//        }
//        else
//        {
//            // Get the current job based on the current index
//            var currentJob = jobs[currentIndex];

//            if (currentIndex == 0)
//            {
//                model.ExportAthena = new ExportAthena
//                { };
//            }
//            else
//            {
//                model.ExportAthena = new ExportAthena
//                {
//                    Id = currentJob.Id,
//                    JobId = currentJob.JobNumber,
//                    //DueDate = currentJob.DueDate,
//                    PurchaseOrderNumber = currentJob.PurchaseOrderNumber,
//                    PurchaseOrderLine = currentJob.PurchaseOrderLine,
//                    OrderId = currentJob.SalesOrderNumber,
//                    CustomerAccountCode = currentJob.CustomerAccountCode,
//                    JobQty = currentJob.JobQuantity.ToString(),
//                    ASCMOrderID = currentJob.AscmOrderId,
//                    EndCustomer = currentJob.EndCustomer,
//                    ActivationSystem = currentJob.ActivationSystem,
//                    ProductType = currentJob.ProductType,
//                    ErpMaterialCode = currentJob.ErpMaterialCode,
//                    //IntegratorPartID = currentJob.IntegratorPartID,
//                    //IntegratorID = currentJob.IntegratorID,
//                    //ActivationType = currentJob.ActivationType,
//                    PartNumberSku = currentJob.PartNumberSku,
//                    //RetailBarcode = currentJob.RetailBarcode,
//                    //RetailBarcodeType = currentJob.RetailBarcodeType,
//                    //Channel = currentJob.Channel
//                };
//            }

//            ViewBag.AllItemsProcessed = false;
//        }
//    }

//    return View(model);
//}












//protected void AthenaJob()
//{
//    if (GridViewOrder.SelectedIndex == -1) // if no index selected then select the first line - index 0
//    {
//        GridViewOrder.SelectedIndex = 0;
//        jobNumber = GridViewOrder.Rows[GridViewOrder.SelectedIndex].Cells[1].Text;
//    }
//    DAL d = new DAL(); //Create new Data Access Layer object from DAL class
//    AthenaJob athenaJob = d.GetAthenaJob(jobNumber); // generate Athena job line object from Job class in DAL

//    PartNumberTextBox.Text = athenaJob.PartNumberSku.ToString();
//    JobNumberTextBox.Text = athenaJob.JobNumber.ToString();
//    OrderNumberTextBox.Text = athenaJob.SalesOrderNumber.ToString();
//    CustomerAccountCodeTextBox.Text = athenaJob.CustomerAccountCode.ToString();
//    PurchaseOrderNumberTextBox.Text = athenaJob.PurchaseOrderNumber.ToString();
//    JobQuantityTextBox.Text = athenaJob.JobQuantity.ToString();
//    AscmOrderIDTextBox.Text = athenaJob.AscmOrderId.ToString();
//    EndCustomerTextBox.Text = athenaJob.EndCustomer.ToString();
//    ActivationSystemTextBox.Text = athenaJob.ActivationSystem.ToString();
//    ProductTypeTextBox.Text = athenaJob.ProductType.ToString();
//    ErpMaterialCodeTextBox.Text = athenaJob.ErpMaterialCode.ToString();


//    lblIntegratorID.Text = athenaJob.IntegratorID.ToString();
//    lblChannel.Text = athenaJob.Channel.ToString();
//    lblPOLineNo.Text = athenaJob.PurchaseOrderLine.ToString();

//    lblIntegratorID.Visible = true;
//    lblChannel.Visible = true;
//    lblPOLineNo.Visible = true;

//    if (lblChannel.Text.ToString() == "Direct")
//    {
//        lblChannel.Visible = true;
//    }
//    else if (lblChannel.Text.ToString() == "NA")
//    {
//        lblChannel.Visible = false;
//    }

//    // Check if CR80
//    if (ActivationSystemTextBox.Text == "CR80")
//    {
//        lblActivationSystemCR80.Text = "true";
//    }

//    // FAI QTYS
//    FaiStartTextBox.Text = "";
//    FaiEndTextBox.Text = "";

//    if (athenaJob.EndCustomer.ToString().ToUpper() == "MICROSOFT")
//    {
//        FaiStartTextBox.Text = "3";
//        FaiEndTextBox.Text = "2";

//        if (athenaJob.IntegratorID.ToString().ToUpper() == "INCOMM" & athenaJob.Channel.ToUpper() == "INDIRECT")
//        {
//            FaiStartTextBox.Text = "3";
//            FaiEndTextBox.Text = "12";
//        }
//    }

//    if (athenaJob.EndCustomer.ToString().ToUpper() == "INTEL SECURITY")
//    {
//        FaiStartTextBox.Text = "1";
//        FaiEndTextBox.Text = "1";

//        lbl_IsIntel.Text = "true"; // Make FileUpload Visible
//        FileUpload.Visible = true;
//        lbl_IsIntel.Text = "true";
//        btnImportFile.Visible = true;
//        lblUploadOrderInstructions.Visible = true;
//        lblUploadOrderInstructions.Text = "Load Intel Launch file";

//        TopLevelSKUTextBox.Visible = true; // Top Level SKU & Product Name
//        TopLevelSKUTextBox.Text = "";
//        lblTopLevelSKU.Visible = true;
//        ProductNameTextBox.Visible = true;
//        ProductNameTextBox.Text = "";
//        lblProductName.Visible = true;
//    }

//    if (athenaJob.ActivationSystem.ToString().ToUpper() == "ENVIROCARD")
//    {
//        JobQuantityTextBox.Text = (athenaJob.JobQuantity + 2).ToString(); // add 2
//        FaiStartTextBox.Text = "NA";
//        FaiEndTextBox.Text = "NA";
//        DenominationTextBox.Text = "0";
//        DenominationCurrencyTextBox.Text = "0";
//        MsRequestNumberTextBox.Text = "";

//        // if Order Qty ends in 21 then do not add extra 2
//        string qty = athenaJob.JobQuantity.ToString();
//        qty = qty.Substring(qty.Length - 2);
//        if (qty == "21")
//        {
//            JobQuantityTextBox.Text = athenaJob.JobQuantity.ToString();
//        }
//        FileUpload.Visible = true;
//        lbl_IsEnvirocard.Text = "true";
//        btnImportFile.Visible = true;
//        lblUploadOrderInstructions.Visible = true;
//    }

//    else if (athenaJob.ActivationSystem.ToString().ToUpper() == "DLC")
//    {
//        string spec = ProductTypeTextBox.Text;
//        int qty = (int)athenaJob.JobQuantity;
//        string printSource = athenaJob.Channel.ToString();
//        JobQuantityTextBox.Text = NumberUpQty.GetQty(spec, qty, printSource).ToString();
//        FaiStartTextBox.Text = "0";
//        FaiEndTextBox.Text = "0";
//        FileUpload.Visible = true;
//        lbl_IsDLC.Text = "true";
//        btnImportFile.Visible = true;
//        lblUploadOrderInstructions.Visible = true;
//    }
//    else if (athenaJob.ActivationSystem.ToString().ToUpper() == "CR80")
//    {
//        int qty = (int)athenaJob.JobQuantity;
//        qty = qty + 5; // add 5
//        JobQuantityTextBox.Text = qty.ToString();
//        FaiStartTextBox.Text = "0";
//        FaiEndTextBox.Text = "0";
//        FileUpload.Visible = true;
//        btnImportFile.Visible = true;
//        lblUploadOrderInstructions.Visible = true;
//    }
//    else
//    {
//        JobQuantityTextBox.Text = athenaJob.JobQuantity.ToString();
//    }


//    if (athenaJob.CustomerAccountCode.ToString() == "1700")
//    {
//        ContractTypeLvTextBox.Text = "Envirocard";
//    }
//    else if (athenaJob.CustomerAccountCode.ToString() == "1774")
//    {
//        ContractTypeLvTextBox.Text = "Multipack POSA";
//    }
//    else if (athenaJob.CustomerAccountCode.ToString() == "1781" || athenaJob.CustomerAccountCode.ToString() == "1784" || athenaJob.CustomerAccountCode.ToString() == "1795")
//    {
//        ContractTypeLvTextBox.Text = "M6 POSA";
//    }

//    // Load Incomm Indirect text boxes if Incomm Indirect is flagged
//    if (athenaJob.IntegratorID.ToUpper() == "INCOMM" && athenaJob.Channel.ToUpper() == "INDIRECT")
//    {
//        WPNDescriptionTextBox.Visible = true; WPNDescriptionTextBox.Text = ""; lblWPNDescription.Visible = true;
//        //DenominationTextBox.Visible = true; DenominationTextBox.Text = ""; lblDenomination.Visible = true;
//        //DenominationCurrencyTextBox.Visible = true; DenominationCurrencyTextBox.Text = ""; lblDenominationCurrency.Visible = true;
//        SOWNoTextBox.Visible = true; SOWNoTextBox.Text = ""; lblSOWNo.Visible = true;
//        WPNTextBox.Visible = true; WPNTextBox.Text = ""; lblWPN.Visible = true;
//    }
//    if (athenaJob.IntegratorID.ToUpper() == "BLACKHAWK" && athenaJob.Channel.ToUpper() == "INDIRECT" && lblBlackhawkPO.Text == "true") // IF a BHN Work Instructions file has been loaded
//    {
//        string[] parts = PartNumberTextBox.Text.Split('_');
//        string partNum = parts.Length > 0 ? parts[0] : string.Empty;

//        string input = BHNWorkInstructions.GetBHNPONumber(partNum, connectStr);
//        input = input.Replace("\"", "");
//        input = input.Replace("\\", "");
//        string result = GetTextAfterSecondB(input);

//        BHNPONumberTextBox.Text = result;

//        FileUpload.Visible = false;
//        btnImportFile.Visible = false;
//        lblUploadOrderInstructions.Visible = false;
//    }
//    // Load Blackhawk PO text box if Blackhawk Indirect is flagged
//    else if (athenaJob.IntegratorID.ToUpper() == "BLACKHAWK" && athenaJob.Channel.ToUpper() == "INDIRECT")// IF a BHN Work Instructions file has NOT been loaded then display the import buttons
//    {

//        BHNPONumberTextBox.Visible = true; BHNPONumberLabel.Visible = true;
//        FileUpload.Visible = true;
//        btnImportFile.Visible = true;
//        lblUploadOrderInstructions.Visible = true;
//        lblUploadOrderInstructions.Text = "Load Blackhawk Work Instructions";
//    }
//    else
//    {
//        WPNDescriptionTextBox.Visible = false; lblWPNDescription.Visible = false;
//        //DenominationTextBox.Visible = false; lblDenomination.Visible = false;
//        //DenominationCurrencyTextBox.Visible = false; lblDenominationCurrency.Visible = false;
//        SOWNoTextBox.Visible = false; lblSOWNo.Visible = false;
//        WPNTextBox.Visible = false; lblWPN.Visible = false;
//    }
//    JobCommentsTextBox.Text = "";

//    if (rbtnIsDach.Checked == true)
//    {
//        DachDescriptionTextBox.Visible = true;
//        lblDachDescription.Visible = true;
//        DachCountryTextBox.Visible = true;
//        lblDachCountry.Visible = true;
//        rfvDachCountryTextBox.Visible = true;
//    }
//}