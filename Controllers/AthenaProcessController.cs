using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using SPV_Loader.Models;
using System.IO;  // Ensure System.IO is being used for file operations
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace SPV_Loader.Controllers
{
    public class AthenaProcessController : Controller
    {
        // Method to process AthenaJob
        public ExportAthena ProcessJob(AthenaViewModel athenaViewModel)
        {
            var exportAthena = new ExportAthena();
            var job = athenaViewModel.AthenaDetails;
            if (job.JobNumber != null)
            {
                exportAthena = new ExportAthena
                {
                    JobId = job.JobNumber,
                    OrderId = job.SalesOrderNumber,
                    PurchaseOrderNumber = job.PurchaseOrderNumber,
                    PurchaseOrderLine = job.PurchaseOrderLine,
                    CustomerAccountCode = job.CustomerAccountCode,
                    JobQty = job.JobQuantity?.ToString(),
                    ASCMOrderID = job.AscmOrderId,
                    EndCustomer = job.EndCustomer,
                    ActivationSystem = job.ActivationSystem,
                    ProductType = job.ProductType,
                    ErpMaterialCode = job.ErpMaterialCode,
                    FAIStart = athenaViewModel.ExportAthena.FAIStart,
                    FAIEnd = athenaViewModel.ExportAthena.FAIEnd,
                    ContractTypeLVId = athenaViewModel.ExportAthena.ContractTypeLVId,
                    PartNumberSku = job.PartNumberSku,
                    JobComments = athenaViewModel.ExportAthena.JobComments,
                    JobTypeLVId = athenaViewModel.ExportAthena.JobTypeLVId,
                    SpecificationLVId = athenaViewModel.ExportAthena.SpecificationLVId,
                    UPC = athenaViewModel.ExportAthena.UPC,
                    AlternativePartNumber = athenaViewModel.ExportAthena.AlternativePartNumber,
                    PackQty = athenaViewModel.ExportAthena.PackQty,
                    BoxQty = athenaViewModel.ExportAthena.BoxQty,
                    PalletQty = athenaViewModel.ExportAthena.PalletQty,
                    Description = athenaViewModel.ExportAthena.Description,
                    IncommRetailer = athenaViewModel.ExportAthena.IncommRetailer,
                    IncommProductDescription = athenaViewModel.ExportAthena.IncommProductDescription,
                    Denomination = athenaViewModel.ExportAthena.Denomination,
                    DenominationCurrency = athenaViewModel.ExportAthena.DenominationCurrency,
                    ArtworkPartNumber = athenaViewModel.ExportAthena.AlternativePartNumber,
                    PackagingGTIN = athenaViewModel.ExportAthena.PackagingGTIN,
                    BHNPONumber = athenaViewModel.ExportAthena.BHNPONumber,
                    MSRequestNumber1 = athenaViewModel.ExportAthena.MSRequestNumber1,
                    MSRequestNumber2 = athenaViewModel.ExportAthena.MSRequestNumber2,
                    MSRequestNumber3 = athenaViewModel.ExportAthena.MSRequestNumber3,
                    MSRequestNumber4 = athenaViewModel.ExportAthena.MSRequestNumber4,
                    MSRequestNumber5 = athenaViewModel.ExportAthena.MSRequestNumber5,
                    PKPN1 = athenaViewModel.ExportAthena.PKPN1,
                    PKPN2 = athenaViewModel.ExportAthena.PKPN2,
                    PKPN3 = athenaViewModel.ExportAthena.PKPN3,
                    PKPN4 = athenaViewModel.ExportAthena.PKPN4,
                    PKPN5 = athenaViewModel.ExportAthena.PKPN5,
                    BOMComment1 = athenaViewModel.ExportAthena.BOMComment1,
                    BOMComment2 = athenaViewModel.ExportAthena.BOMComment2,
                    BOMComment3 = athenaViewModel.ExportAthena.BOMComment3,
                    BOMComment4 = athenaViewModel.ExportAthena.BOMComment4,
                    BOMComment5 = athenaViewModel.ExportAthena.BOMComment5,
                };

                //exportAthena.JobComments = athenaViewModel.ExportAthena.JobComments;
                //exportAthena.AlternativePartNumber = athenaViewModel.ExportAthena.AlternativePartNumber;
                //exportAthena.IncommProductDescription = athenaViewModel.ExportAthena.IncommProductDescription;

                if (job.CustomerAccountCode.ToString() == "1781" || job.CustomerAccountCode.ToString() == "1784" || job.CustomerAccountCode.ToString() == "1795")
                {
                    exportAthena.ContractTypeLVId = "M6 POSA";
                }

                if (job.CustomerAccountCode.ToString() == "1700")
                {
                    exportAthena.ContractTypeLVId = "Envirocard";
                }

                if (job.CustomerAccountCode.ToString() == "1774")
                {
                    exportAthena.ContractTypeLVId = "Multipack POSA";
                }

                if (!string.IsNullOrEmpty(job.EndCustomer) && job.EndCustomer.ToUpper() == "MICROSOFT")
                {
                    exportAthena.FAIStart = "3";
                    exportAthena.FAIEnd = "2";

                    if (!string.IsNullOrEmpty(job.IntegratorID) && job.IntegratorID.ToString().ToUpper() == "INCOMM" && !string.IsNullOrEmpty(job.Channel) && job.Channel.ToUpper() == "INDIRECT")
                    {
                        exportAthena.FAIStart = "3";
                        exportAthena.FAIEnd = "12";
                    }

                    if (!string.IsNullOrEmpty(job.IntegratorID) && job.IntegratorID.ToString().ToUpper() == "BLACKHAWK" && !string.IsNullOrEmpty(job.Channel) && job.Channel.ToUpper() == "INDIRECT")
                    {
                        using (SpvLoaderEntities context = new SpvLoaderEntities())
                        {
                            var BlackhawkWorkInstructions = context.BHNWorkInstructions.ToList();
                            var partnumber = "";
                            if (job.PartNumberSku != null)
                            {
                                partnumber = job.PartNumberSku.Substring(0, job.PartNumberSku.IndexOf("_"));
                            }
                            foreach (var b in BlackhawkWorkInstructions)
                            {
                                if (b != null)
                                {
                                    if (!string.IsNullOrEmpty(partnumber) && partnumber == b.SKU)
                                    {
                                        exportAthena.BHNPONumber = b.PO;
                                    }
                                }
                            }
                        }
                    }

                }

                if (!string.IsNullOrEmpty(job.EndCustomer) && job.EndCustomer.ToUpper() == "INTEL SECURITY")
                {
                    exportAthena.FAIStart = "1";
                    exportAthena.FAIEnd = "1";
                    string partNumber = athenaViewModel.AthenaDetails.PartNumberSku;
                    partNumber = partNumber.Substring(0, partNumber.IndexOf("_"));

                    string CSVFilePathName = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Intel.csv");

                    if (System.IO.File.Exists(CSVFilePathName))  // Check if the file exists
                    {
                        try
                        {
                            DataTable intelLookup = new DataTable();

                            // Use TextFieldParser to read CSV files correctly handling quoted fields
                            using (TextFieldParser parser = new TextFieldParser(CSVFilePathName))
                            {
                                parser.TextFieldType = FieldType.Delimited;
                                parser.SetDelimiters(",");  // Define the comma as the delimiter
                                parser.HasFieldsEnclosedInQuotes = true;  // Set this to true to handle quotes

                                // If there are lines to read, process them
                                if (!parser.EndOfData)
                                {
                                    // Read the first line to get headers
                                    string[] fields = parser.ReadFields();
                                    if (fields != null)
                                    {
                                        int cols = fields.Length;

                                        // Add columns to the DataTable using the header names, in lowercase
                                        for (int i = 0; i < cols; i++)
                                            intelLookup.Columns.Add(fields[i].ToLower(), typeof(string));
                                    }

                                    // Read remaining lines and populate the DataTable
                                    while (!parser.EndOfData)
                                    {
                                        fields = parser.ReadFields();
                                        if (fields != null)
                                        {
                                            DataRow row = intelLookup.NewRow();
                                            for (int f = 0; f < fields.Length; f++)
                                            {
                                                row[f] = fields[f];
                                            }
                                            intelLookup.Rows.Add(row);
                                        }
                                    }
                                }
                            }

                            // Perform lookup to find the matching part number
                            foreach (DataRow row in intelLookup.Rows)
                            {
                                string lookupPartNumber = row[6].ToString();  // Assuming the first column contains the part number
                                lookupPartNumber = Regex.Replace(lookupPartNumber, @"[^0-9-]", "");

                                if (partNumber == lookupPartNumber)
                                {
                                    string A = "BusinessModel = " + row[15].ToString() + " " + "\n";
                                    string B = "ScratchON/OFF = " + row[7].ToString() + " " + "\n";
                                    string C = "PartNumber = " + row[6].ToString() + " " + "\n";
                                    string D = "TopLevelSku = " + row[4].ToString() + " " + "\n";
                                    string E = "CartonRetailCode = " + row[31].ToString() + "\n";
                                    string F = "QrCodeRequired = " + row[45].ToString() + " ";
                                    string jobComment = A + B + C + D + E + F;
                                    exportAthena.JobComments = jobComment;
                                    exportAthena.AlternativePartNumber = row[4].ToString().Replace("\"", ""); // remove quotes
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing file: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The specified CSV file does not exist.");
                    }
                }

                if (!string.IsNullOrEmpty(job.ActivationSystem) && job.ActivationSystem.ToUpper() == "DLC")
                {
                    exportAthena.FAIStart = "0";
                    exportAthena.FAIEnd = "0";

                    string spec = athenaViewModel.AthenaDetails.ProductType;
                    int qty = (int)athenaViewModel.AthenaDetails.JobQuantity;
                    string printSource = athenaViewModel.AthenaDetails.Channel;
                    int codeQty = 0;

                    // Load Lookup Table
                    string CSVFilePathName = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/DLC_Digital-v-Litho_Rev2.csv");

                    if (System.IO.File.Exists(CSVFilePathName))  // Check if the file exists
                    {
                        try
                        {
                            // Read all lines from the CSV file using System.IO.File
                            string[] lines = System.IO.File.ReadAllLines(CSVFilePathName);

                            if (lines.Length > 0)
                            {
                                // Split the first line (header) to get the column names
                                string[] fields = lines[0].Split(',');
                                int cols = fields.Length;
                                DataTable codesLookup = new DataTable();

                                // Add columns to the DataTable using the header names, in lowercase
                                for (int i = 0; i < cols; i++)
                                    codesLookup.Columns.Add(fields[i].ToLower(), typeof(string));

                                // Populate the DataTable with the remaining lines of the CSV
                                for (int i = 1; i < lines.Length; i++)
                                {
                                    fields = lines[i].Split(',');
                                    DataRow row = codesLookup.NewRow();

                                    for (int f = 0; f < cols; f++)
                                        row[f] = fields[f];

                                    codesLookup.Rows.Add(row);
                                }

                                // Perform lookup to find the matching `spec` and calculate `codeQty`
                                foreach (DataRow row in codesLookup.Rows)
                                {
                                    string rowSpec = row[0].ToString();  // Assuming the first column contains the `spec`

                                    if (spec == rowSpec)
                                    {
                                        // Convert the appropriate column to integer to get the `numberUp` value
                                        int numberUp = Convert.ToInt32(row[3]);  // Assuming the 4th column contains the "Number up Digital" field
                                        codeQty = (int)Math.Ceiling((qty + 5) / (double)numberUp) * numberUp;
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing file: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The specified CSV file does not exist.");
                    }

                    exportAthena.JobQty = codeQty.ToString();
                }

                if (!string.IsNullOrEmpty(job.ActivationSystem) && job.ActivationSystem.ToUpper() == "CR80")
                {
                    exportAthena.FAIStart = "0";
                    exportAthena.FAIEnd = "0";
                    int qty = Convert.ToInt32(job.JobQuantity);
                    exportAthena.JobQty = (qty + 5).ToString();
                }

                return exportAthena;
            }
            return exportAthena;
        }
    }
}




