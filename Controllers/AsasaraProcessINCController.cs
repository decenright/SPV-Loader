using SPV_Loader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SPV_Loader.Controllers
{
    public class AsasaraProcessINCController : Controller
    {
        // GET: AsasaraProcessINC
        public ActionResult Index()
        {
            return View();
        }

        public ExportAsasara ProcessJob(AsasaraViewModel asasaraViewModel)
        {
            var exportAsasara = new ExportAsasara();
            var job = asasaraViewModel.AsasaraDetails;
            if (job.JobNumber != null)
            {
                var country = job.Country;
                var integrator = job.Integrator_Share;

                exportAsasara.orderID = job.SalesOrder.ToString();
                exportAsasara.jobID = job.JobNumber.ToString();
                exportAsasara.jobQty = (job.BuildQty - job.Test_Quantity_Sandbox_Dev).ToString();

                // Integrator Name
                exportAsasara.integratorName = job.Integrator_Share.ToUpper();

                if (exportAsasara.integratorName.ToUpper() == "INCOMM")
                {
                    exportAsasara.integratorName = "Incomm";
                }

                exportAsasara.activationType = job.Partner_Encoding_Type.ToString(); 
                if (exportAsasara.activationType.ToUpper() == "TIBIDONO CODE128")
                {
                    exportAsasara.activationType = "16Serial128";
                }

                exportAsasara.regionID = job.Partner_Encoding_Type.ToString();
                if (exportAsasara.regionID.ToUpper() == "CODE 128C 16 DIGITS")
                {
                    exportAsasara.regionID = "16Serial128";
                }
                if (exportAsasara.regionID.ToUpper() == "TIBIDONO CODE128")
                {
                    exportAsasara.regionID = "16Serial128";
                }

                exportAsasara.pptQty = job.Test_Quantity_Production_Data_Proof.ToString();
                if (job.Test_Quantity_Production_Data_Proof > 200)
                {
                    exportAsasara.pptQty = "ERROR: PPT Qty must be less than 200";
                }

                exportAsasara.denomination = job.Denomination.ToString().Substring(job.Denomination.ToString().IndexOf(':') + 2); ; // Denomination
                if (exportAsasara.denomination.Contains("-"))
                {
                    exportAsasara.denomination = "0";
                }

                exportAsasara.currency = Currency.GetCurrency(job.Country.ToString());
                exportAsasara.retailBarcode = job.Production_UPC.ToString();
                exportAsasara.customerJobNumber = job.PartNumber.ToString();
                exportAsasara.integratorID = job.Integrator_Product_ID.ToString();

                // product Description changed to label description
                exportAsasara.productDescription = job.NAN___Shipping_PO___Product_Packing_Label.ToString();

                exportAsasara.maskID = "MASK02_INCOMMGG";
                exportAsasara.dummyRecords = "False";
                exportAsasara.dummyRecordsCount = "0";
                exportAsasara.dummyRecordsEvery = "0";
                exportAsasara.packQty = job.Cards_Per_Pack.ToString();
                exportAsasara.caseQty = job.Cards_Per_Carton.ToString();
                exportAsasara.palletQty = "44800";

                // Expiry Date
                exportAsasara.expiryDate = job.Packaging___Encoding_Identifier.ToString();
                if (exportAsasara.expiryDate == "")
                {
                    exportAsasara.expiryDate = "4912";
                }

                exportAsasara.whiteCardTestQty = job.Test_Quantity_Sandbox_Dev.ToString();

                // Retail Barcode Type
                exportAsasara.retailBarcode = job.Production_UPC.ToString();
                if (exportAsasara.retailBarcode.Length == 12)
                {
                    exportAsasara.retailBarcodeType = "UPC";
                }
                else if (exportAsasara.retailBarcode.Length == 13)
                {
                    exportAsasara.retailBarcodeType = "EAN";
                }

                exportAsasara.barcodeStyleType = "Code 128C";
                exportAsasara.alternativePartNumber = job.Project_ID.ToString();
                exportAsasara.country = job.Country.ToString();
                exportAsasara.eanBundle = job.BHN_Pack_Description___Packing_UPC.ToString();
                exportAsasara.eanBox = job.BHN_Pack_Description___Packing_UPC.ToString();
                exportAsasara.eanPallet = job.BHN_Pack_Description___Packing_UPC.ToString();
                exportAsasara.DODHumanFontTypeID= "Yes";
                exportAsasara.partnerTextBox = "Google Play";
                exportAsasara.palletTypeID = "EURO";

                // Recipient Address
                string s = job.Ship_To_Location_Text.ToString(); // Ship To Address
                string[] addressParts = s.Split(',');
                int c = addressParts.Count();
                if (c >= 1) { exportAsasara.recipientAddress1 = addressParts[0].Trim(); }
                else { exportAsasara.recipientAddress1 = ""; }

                if (c >= 2) { exportAsasara.recipientAddress2 = addressParts[1].Trim(); }
                else { exportAsasara.recipientAddress2 = ""; }

                if (c >= 3) { exportAsasara.recipientAddress3 = addressParts[2].Trim(); }
                else { exportAsasara.recipientAddress3 = ""; }

                if (c >= 4) { exportAsasara.recipientAddress4 = addressParts[3].Trim(); }
                else { exportAsasara.recipientAddress4 = ""; }

                if (c >= 5) { exportAsasara.recipientAddress5 = addressParts[4].Trim(); }
                else { exportAsasara.recipientAddress5= ""; }

                if (c >= 6) { exportAsasara.recipientAddress6 = addressParts[5].Trim(); }
                else { exportAsasara.recipientAddress6 = ""; }

                exportAsasara.FAIQty = ((int)job.Google_FAI_Quantity + (int)job.Integrator_FAI_Quantity).ToString();
                exportAsasara.OCR = job.OCR.ToString();

                // Label Style
                string retailBarcode = job.Production_UPC.ToString();
                if (retailBarcode.Length == 12)
                {
                    exportAsasara.labelStyle = "INCOMM UPC12";
                }
                else if (retailBarcode.Length == 13)
                {
                    exportAsasara.labelStyle = "INCOMM EAN13";
                }

                string activationMode = job.Internal_Activation.ToString();
                if (activationMode == "Barcode")
                {
                    exportAsasara.activationMode = "Barcode Only";
                }
                if (activationMode == "Barcode+Magstripe")
                {
                    exportAsasara.activationMode = "Hybrid";
                }
                if (activationMode == "Hybrid")
                {
                    exportAsasara.activationMode = "Hybrid";
                }

                exportAsasara.pinFile = RandomString(6);

                // White Test Card Pin File - random code
                if (job.Test_Quantity_Sandbox_Dev.ToString() != "")
                {
                    exportAsasara.WCTPinFile = RandomString(6);
                }
                else
                {
                    exportAsasara.WCTPinFile = "";
                }

                return exportAsasara;
            }
            return exportAsasara;
        }

        private static Random random = new Random();

        public static string RandomString(int length)// Create random alphanumeric code for pinFile and WCTpinFile tags
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }


    }
}