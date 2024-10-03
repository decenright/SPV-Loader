using SPV_Loader;
using SPV_Loader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace SPV_Loader.Controllers
{
    public class AsasaraProcessEPYController : Controller
    {
        // GET: AsasaraProcessEPY
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
                exportAsasara.integratorName = job.Integrator_Share.ToUpper();

                //activationType
                exportAsasara.activationType = job.Partner_Encoding_Type.ToString();
                if (job.Partner_Encoding_Type.ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
                {
                    exportAsasara.activationType = "Hybrid GS1128/Mag 19 digits";
                }

                exportAsasara.regionID = "EMEA";
                exportAsasara.pptQty = job.Test_Quantity_Production_Data_Proof.ToString();

                //Denomination
                string denomination = job.Denomination.Substring(job.Denomination.IndexOf(':') + 2);
                if (denomination.Contains("-"))
                {
                    denomination = "0";
                }
                exportAsasara.denomination = denomination;
                exportAsasara.currency = Currency.GetCurrency(job.Country.ToString());
                exportAsasara.retailBarcode = job.Production_UPC.ToString();
                exportAsasara.customerJobNumber = job.PartNumber;
                exportAsasara.integratorID = job.Integrator_Product_ID;

                //Product Description
                exportAsasara.productDescription = job.Identifier.ToString(); // Product Description
                int indexOfFirstDash = exportAsasara.productDescription.IndexOf('-');
                exportAsasara.productDescription = exportAsasara.productDescription.Substring(indexOfFirstDash + 1);// remove first dash 
                exportAsasara.productDescription = exportAsasara.productDescription.Substring(exportAsasara.productDescription.IndexOf('-') + 2);// get chars after second dash
                exportAsasara.maskID = "MASK03_ARVATO_EPAYGG";
                exportAsasara.dummyRecords = "False";
                exportAsasara.dummyRecordsCount = "0";
                exportAsasara.dummyRecordsEvery = "0";
                exportAsasara.packQty = job.Cards_Per_Pack.ToString();
                exportAsasara.caseQty = job.Cards_Per_Carton.ToString();
                exportAsasara.palletQty = "44800";
                exportAsasara.expiryDate = "4912";
                exportAsasara.whiteCardTestQty = job.Test_Quantity_Sandbox_Dev.ToString();

                //Retail Barcode Type
                string productionUPC = job.Production_UPC.ToString();
                if (productionUPC.Length == 12)
                {
                    exportAsasara.retailBarcodeType = "UPC";
                }
                else if (productionUPC.Length == 13)
                {
                    exportAsasara.retailBarcodeType = "EAN";
                }

                //Barcode Style Type - extract from Partner_Encoding_Type)
                if (job.Partner_Encoding_Type.ToString() == "GS1-128 16 digits" || job.Partner_Encoding_Type.ToString() == "GS1-128 16 Digits")
                {
                    exportAsasara.barcodeStyleType = "GS1-128";
                }
                else if (job.Partner_Encoding_Type.ToString() == "I2of5 16 digits" || job.Partner_Encoding_Type.ToString() == "I2of5 16 Digits")
                {
                    exportAsasara.barcodeStyleType = "I2of5";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128C 16 digits" || job.Partner_Encoding_Type == "Code 128C 16 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128C";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Hybrid Code128/Mag 19 digits" || job.Partner_Encoding_Type.ToString() == "Hybrid Code128/Mag 19 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128A";
                }
                else if (job.Partner_Encoding_Type.ToString().ToUpper() == "HYBRID GS1128/MAG 19 DIGITS" || job.Partner_Encoding_Type.ToString().ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
                {
                    exportAsasara.barcodeStyleType = "GS1-128";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128C 32 digits w/o EAN13 (Netto)" || job.Partner_Encoding_Type.ToString() == "Code 128C 32 Digits w/o EAN13 (Netto)")
                {
                    exportAsasara.barcodeStyleType = "Code 128C";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128C 32 digits" || job.Partner_Encoding_Type.ToString() == "Code 128C 32 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128C";
                }
                else if (job.Partner_Encoding_Type.ToString() == "I2of5 18 digits" || job.Partner_Encoding_Type.ToString() == "I2of5 18 Digits")
                {
                    exportAsasara.barcodeStyleType = "I2of5";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128A 9 digits" || job.Partner_Encoding_Type.ToString() == "Code 128A 9 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128A";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128C 16 digits" || job.Partner_Encoding_Type.ToString() == "Code 128C 16 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128C";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128A 16 digits" || job.Partner_Encoding_Type.ToString() == "Code 128A 16 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128A";
                }
                else if (job.Partner_Encoding_Type.ToString() == "GS1-128 34 digits" || job.Partner_Encoding_Type.ToString() == "GS1-128 34 Digits")
                {
                    exportAsasara.barcodeStyleType = "GS1-128";
                }
                else if (job.Partner_Encoding_Type.ToString() == "Code 128 16 digits" || job.Partner_Encoding_Type.ToString() == "Code 128 16 Digits")
                {
                    exportAsasara.barcodeStyleType = "Code 128C";
                }
                else if (job.Partner_Encoding_Type.ToString() == "I2of5 16 Digits (Coop CH)" || job.Partner_Encoding_Type.ToString() == "I2of5 16 digits (Coop CH)")
                {
                    exportAsasara.barcodeStyleType = "I2of5";
                }
                else
                {
                    exportAsasara.barcodeStyleType = "No match - enter text manually";

                }

                exportAsasara.alternativePartNumber = job.NAN___Shipping_PO___Product_Packing_Label.ToString();
                exportAsasara.country = job.Country.ToString();

                //EAN BUNDLE
                exportAsasara.eanBundle = job.Pack_EAN.ToString();
                if (exportAsasara.eanBundle.ToUpper() == "N/A")
                {
                    exportAsasara.eanBundle = "0";
                }

                exportAsasara.eanBox = job.Case_EAN.ToString();

                //EAN Pallet
                exportAsasara.eanPallet = job.Pallet_EAN.ToString();
                if (exportAsasara.eanPallet.ToUpper() == "N/A")
                {
                    exportAsasara.eanPallet = "0";
                }

                exportAsasara.DODHumanFontTypeID = "Yes";
                exportAsasara.partnerTextBox = "Google Play";

                //Partner Code
                String brand = job.BHN_Brand_Code___BHN_1st_Case_Quantity___PID_Number; // Brand
                if (brand.Length >= 4)
                {
                    exportAsasara.partnerCode = brand.Substring(0, 4);
                }
                else
                {
                    exportAsasara.partnerCode = job.BHN_Brand_Code___BHN_1st_Case_Quantity___PID_Number.ToString();
                }

                exportAsasara.brand = "";
                exportAsasara.VASBoxDescription = "";
                exportAsasara.palletTypeID = "EURO";

                string s = job.Ship_To_Location_Text.ToString(); // Ship To Address
                string[] addressParts = s.Split(',');
                int c = addressParts.Count();

                if (c >= 1) { exportAsasara.recipientAddress1 = addressParts[0].Trim(); }
                else { exportAsasara.recipientAddress1 = ""; }

                if (c >= 2) { exportAsasara.recipientAddress2 = addressParts[1].Trim(); }
                else { exportAsasara.recipientAddress2 = ""; }

                if (c >= 3) { exportAsasara.recipientAddress3 = addressParts[2].Trim(); }
                else { exportAsasara.recipientAddress3 = ""; }

                if (c >= 4) exportAsasara.recipientAddress4 = addressParts[3].Trim();
                else { exportAsasara.recipientAddress4 = ""; }

                if (c >= 5) { exportAsasara.recipientAddress5 = addressParts[4].Trim(); }
                else { exportAsasara.recipientAddress5 = ""; }

                if (c >= 6) { exportAsasara.recipientAddress6 = addressParts[5].Trim(); }
                else { exportAsasara.recipientAddress6 = ""; }

                exportAsasara.codeFormat = job.Partner_Encoding_Type.ToString();
                if (job.Partner_Encoding_Type.ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
                {
                    exportAsasara.codeFormat = "Hybrid GS1128/Mag 19 digits";
                }
                if (job.Partner_Encoding_Type.ToUpper() == "SPECIFIC VAS D CODE128 32 DIG" || job.Partner_Encoding_Type.ToUpper() == "SPECIFIC VAS D CODE128 32 DIGITS")
                {
                    exportAsasara.codeFormat = "Code 128C 32 digits";
                }

                exportAsasara.vasType = job.Label_Spec.ToString();
                if (exportAsasara.vasType == "V-A")
                {
                    exportAsasara.vasType = "VAS A";
                }
                if (exportAsasara.vasType == "V-B")
                {
                    exportAsasara.vasType = "VAS B";
                }
                if (exportAsasara.vasType == "V-C")
                {
                    exportAsasara.vasType = "VAS C";
                }
                if (exportAsasara.vasType == "V-D")
                {
                    exportAsasara.vasType = "VAS D";
                }
                if (exportAsasara.vasType == "V-E")
                {
                    exportAsasara.vasType = "VAS E";
                }

                int fai1 = (int)job.Google_FAI_Quantity;
                int fai2 = (int)job.Integrator_FAI_Quantity;
                int fai = fai1 + fai2;
                exportAsasara.FAIQty = fai.ToString();

                exportAsasara.OCR = job.OCR.ToString();

                //Label Style
                if (job.Label_Spec.ToString() == "V-A" || job.Label_Spec.ToString() == "VAS-A")
                {
                    exportAsasara.labelStyle = "EPAY VAS A";
                }
                else if (job.Label_Spec.ToString() == "V-B" || job.Label_Spec.ToString() == "VAS-B")
                {
                    exportAsasara.labelStyle = "EPAY VAS B";
                }
                else if (job.Label_Spec.ToString() == "V-C" || job.Label_Spec.ToString() == "VAS-C")
                {
                    exportAsasara.labelStyle = "EPAY VAS C";
                }
                else if (job.Label_Spec.ToString() == "V-D" || job.Label_Spec.ToString() == "VAS-D")
                {
                    exportAsasara.labelStyle = "EPAY VAS D";
                }
                else if (job.Label_Spec.ToString() == "V-E" || job.Label_Spec.ToString() == "VAS-E")
                {
                    exportAsasara.labelStyle = "EPAY VAS E";
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

        private static string ActivationType(AsasaraViewModel asasaraViewModel)
        {
            string activationType = "";
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

            return activationType;
        }
    }
}


//if (integrator.ToUpper() == "EPAY")
//{
//    AsasaraJob epayJob = d.GetJob(integratorID); // generate Epay job line object from Job class in DAL

//    activationTypeTextBox.ForeColor = System.Drawing.Color.Black; // reset if previous line item threw the new activation type error
//    activationTypeTextBox.Font.Bold = false;
//    customerJobNumberTextBox.Text = epayJob.PartNumber.ToString();
//    jobIDTextBox.Text = epayJob.JobNumber.ToString();
//    orderIDTextBox.Text = epayJob.SalesOrder.ToString();
//    tbxCustomerAccountCode.Text = epayJob.CustomerAccountCode.ToString();
//    integratorNameTextBox.Text = epayJob.Integrator_Share.ToString().ToUpper();
//    maskIDTextBox.Text = "MASK03_ARVATO_EPAYGG";
//    dummyRecordsTextBox.Text = "False";
//    dummyRecordsCountTextBox.Text = "0";
//    dummyRecordsEveryTextBox.Text = "0";
//    DODHumanFontTypeIDComboBox.Text = "Yes";
//    palletTypeIDComboBox.Text = "EURO";
//    denominationTextBox.Text = epayJob.Denomination.Substring(epayJob.Denomination.IndexOf(':') + 2);
//    if (denominationTextBox.Text.Contains("-"))
//    {
//        denominationTextBox.Text = "0";
//    }
//    currencyTextBox.Text = Currency.GetCurrency(epayJob.Country.ToString());
//    retailBarcodeTextBox.Text = epayJob.Production_UPC.ToString();
//    productDescriptionTextBox.Text = epayJob.Identifier.ToString(); // Product Description
//    int indexOfFirstDash = productDescriptionTextBox.Text.IndexOf('-');
//    productDescriptionTextBox.Text = productDescriptionTextBox.Text.Substring(indexOfFirstDash + 1);// remove first dash 
//    productDescriptionTextBox.Text = productDescriptionTextBox.Text.Substring(productDescriptionTextBox.Text.IndexOf('-') + 2);// get chars after second dash
//    packQtyTextBox.Text = epayJob.Cards_Per_Pack.ToString();
//    caseQtyTextBox.Text = epayJob.Cards_Per_Carton.ToString();
//    palletQtyTextBox.Text = "44800";
//    expiryDateTextBox.Text = "4912";
//    alternativePartNumberTextBox.Text = epayJob.NAN___Shipping_PO___Product_Packing_Label.ToString();
//    countryTextBox.Text = epayJob.Country.ToString();
//    eanBundleTextBox.Text = epayJob.Pack_EAN.ToString();
//    if (eanBundleTextBox.Text.ToUpper() == "N/A")
//    {
//        eanBundleTextBox.Text = "0";
//    }
//    eanBoxTextBox.Text = epayJob.Case_EAN.ToString();
//    eanPalletTextBox.Text = epayJob.Pallet_EAN.ToString();
//    if (eanPalletTextBox.Text.ToUpper() == "N/A")
//    {
//        eanPalletTextBox.Text = "0";
//    }

//    BHNPONumberTextBox.Visible = false;
//    lblBhnPoNumber.Visible = false;

//    string s = epayJob.Ship_To_Location_Text.ToString(); // Ship To Address
//    string[] addressParts = s.Split(',');
//    int c = addressParts.Count();

//    if (c >= 1) { recipientAddress1TextBox.Text = addressParts[0].Trim(); }
//    else { recipientAddress1TextBox.Text = ""; }

//    if (c >= 2) { recipientAddress2TextBox.Text = addressParts[1].Trim(); }
//    else { recipientAddress2TextBox.Text = ""; }

//    if (c >= 3) { recipientAddress3TextBox.Text = addressParts[2].Trim(); }
//    else { recipientAddress3TextBox.Text = ""; }

//    if (c >= 4) recipientAddress4TextBox.Text = addressParts[3].Trim();
//    else { recipientAddress4TextBox.Text = ""; }

//    if (c >= 5) { recipientAddress5TextBox.Text = addressParts[4].Trim(); }
//    else { recipientAddress5TextBox.Text = ""; }

//    if (c >= 6) { recipientAddress6TextBox.Text = addressParts[5].Trim(); }
//    else { recipientAddress6TextBox.Text = ""; }

//    jobQtyTextBox.Text = (epayJob.BuildQty - epayJob.Test_Quantity_Sandbox_Dev).ToString();

//    activationTypeTextBox.Text = epayJob.Partner_Encoding_Type.ToString();
//    if (epayJob.Partner_Encoding_Type.ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
//    {
//        activationTypeTextBox.Text = "Hybrid GS1128/Mag 19 digits";
//    }

//    regionIDTextBox.Text = "EMEA";
//    pptQtyTextBox.Text = epayJob.Test_Quantity_Production_Data_Proof.ToString();
//    integratorIDTextBox.Text = epayJob.Integrator_Product_ID.ToString();
//    whiteCardTestQtyTextBox.Text = epayJob.Test_Quantity_Sandbox_Dev.ToString();

//    //Barcode Style Type - extract from Partner_Encoding_Type)
//    if (epayJob.Partner_Encoding_Type.ToString() == "GS1-128 16 digits" || epayJob.Partner_Encoding_Type.ToString() == "GS1-128 16 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "GS1-128";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "I2of5 16 digits" || epayJob.Partner_Encoding_Type.ToString() == "I2of5 16 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "I2of5";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128C 16 digits" || epayJob.Partner_Encoding_Type == "Code 128C 16 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128C";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Hybrid Code128/Mag 19 digits" || epayJob.Partner_Encoding_Type.ToString() == "Hybrid Code128/Mag 19 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128A";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString().ToUpper() == "HYBRID GS1128/MAG 19 DIGITS" || epayJob.Partner_Encoding_Type.ToString().ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
//    {
//        barcodeStyleTypeTextBox.Text = "GS1-128";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128C 32 digits w/o EAN13 (Netto)" || epayJob.Partner_Encoding_Type.ToString() == "Code 128C 32 Digits w/o EAN13 (Netto)")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128C";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128C 32 digits" || epayJob.Partner_Encoding_Type.ToString() == "Code 128C 32 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128C";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "I2of5 18 digits" || epayJob.Partner_Encoding_Type.ToString() == "I2of5 18 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "I2of5";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128A 9 digits" || epayJob.Partner_Encoding_Type.ToString() == "Code 128A 9 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128A";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128C 16 digits" || epayJob.Partner_Encoding_Type.ToString() == "Code 128C 16 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128C";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128A 16 digits" || epayJob.Partner_Encoding_Type.ToString() == "Code 128A 16 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128A";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "GS1-128 34 digits" || epayJob.Partner_Encoding_Type.ToString() == "GS1-128 34 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "GS1-128";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "Code 128 16 digits" || epayJob.Partner_Encoding_Type.ToString() == "Code 128 16 Digits")
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128C";
//    }
//    else if (epayJob.Partner_Encoding_Type.ToString() == "I2of5 16 Digits (Coop CH)" || epayJob.Partner_Encoding_Type.ToString() == "I2of5 16 digits (Coop CH)")
//    {
//        barcodeStyleTypeTextBox.Text = "I2of5";
//    }
//    else
//    {
//        lblErrorMessage.Text = "No match for Barcode Style Type - enter text manually";
//        lblErrorMessage.Visible = true;
//        barcodeStyleTypeTextBox.Text = "";
//        barcodeStyleTypeTextBox.BackColor = System.Drawing.Color.LightCoral;
//    }

//    int fai1 = (int)epayJob.Google_FAI_Quantity;
//    int fai2 = (int)epayJob.Integrator_FAI_Quantity;
//    int fai = fai1 + fai2;
//    FAIQtyTextBox.Text = fai.ToString();

//    OCRTextBox.Text = epayJob.OCR.ToString();

//    //Label Style
//    if (epayJob.Label_Spec.ToString() == "V-A" || epayJob.Label_Spec.ToString() == "VAS-A")
//    {
//        labelStyleTextBox.Text = "EPAY VAS A";
//    }
//    else if (epayJob.Label_Spec.ToString() == "V-B" || epayJob.Label_Spec.ToString() == "VAS-B")
//    {
//        labelStyleTextBox.Text = "EPAY VAS B";
//    }
//    else if (epayJob.Label_Spec.ToString() == "V-C" || epayJob.Label_Spec.ToString() == "VAS-C")
//    {
//        labelStyleTextBox.Text = "EPAY VAS C";
//    }
//    else if (epayJob.Label_Spec.ToString() == "V-D" || epayJob.Label_Spec.ToString() == "VAS-D")
//    {
//        labelStyleTextBox.Text = "EPAY VAS D";
//    }
//    else if (epayJob.Label_Spec.ToString() == "V-E" || epayJob.Label_Spec.ToString() == "VAS-E")
//    {
//        labelStyleTextBox.Text = "EPAY VAS E";
//    }

//    string activationMode = epayJob.Internal_Activation.ToString();
//    if (activationMode == "Barcode")
//    {
//        activationModeTextBox.Text = "Barcode Only";
//    }
//    if (activationMode == "Barcode+Magstripe")
//    {
//        activationModeTextBox.Text = "Hybrid";
//    }
//    if (activationMode == "Hybrid")
//    {
//        activationModeTextBox.Text = "Hybrid";
//    }

//    string productionUPC = epayJob.Production_UPC.ToString();
//    if (productionUPC.Length == 12)
//    {
//        retailBarcodeTypeTextBox.Text = "UPC";
//    }
//    else if (productionUPC.Length == 13)
//    {
//        retailBarcodeTypeTextBox.Text = "EAN";
//    }

//    partnerTextBox.Text = "Google Play";

//    String brand = epayJob.BHN_Brand_Code___BHN_1st_Case_Quantity___PID_Number; // Brand
//    if (brand.Length >= 4)
//    {
//        partnerCodeTextBox.Text = brand.Substring(0, 4);
//    }
//    else
//    {
//        partnerCodeTextBox.Text = epayJob.BHN_Brand_Code___BHN_1st_Case_Quantity___PID_Number.ToString();
//    }

//    brandTextBox.Text = "";
//    VASBoxDescriptionTextBox.Text = "";

//    codeFormatTextBox.Text = epayJob.Partner_Encoding_Type.ToString();
//    if (epayJob.Partner_Encoding_Type.ToUpper() == "HYBRID GS1/MAG 19 DIGITS")
//    {
//        codeFormatTextBox.Text = "Hybrid GS1128/Mag 19 digits";
//    }
//    if (epayJob.Partner_Encoding_Type.ToUpper() == "SPECIFIC VAS D CODE128 32 DIG" || epayJob.Partner_Encoding_Type.ToUpper() == "SPECIFIC VAS D CODE128 32 DIGITS")
//    {
//        codeFormatTextBox.Text = "Code 128C 32 digits";
//    }

//    vasTypeTextBox.Text = epayJob.Label_Spec.ToString();
//    if (vasTypeTextBox.Text == "V-A")
//    {
//        vasTypeTextBox.Text = "VAS A";
//    }
//    if (vasTypeTextBox.Text == "V-B")
//    {
//        vasTypeTextBox.Text = "VAS B";
//    }
//    if (vasTypeTextBox.Text == "V-C")
//    {
//        vasTypeTextBox.Text = "VAS C";
//    }
//    if (vasTypeTextBox.Text == "V-D")
//    {
//        vasTypeTextBox.Text = "VAS D";
//    }
//    if (vasTypeTextBox.Text == "V-E")
//    {
//        vasTypeTextBox.Text = "VAS E";
//    }

//    VASPackDescriptionTextBox.Text = "";

//    pinFileTextBox.Text = RandomString(6);

//    if (epayJob.Test_Quantity_Sandbox_Dev.ToString() != "")
//    {
//        WCTPinFileTextBox.Text = RandomString(6);
//    }
//    else
//    {
//        WCTPinFileTextBox.Text = "";
//    }

//    checkNewActivation();

//    saveForExport();// Save to AsasaraExport table
//}