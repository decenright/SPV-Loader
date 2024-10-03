using SPV_Loader;
using SPV_Loader.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using static SPV_Loader.Controllers.ImportOrderController;

namespace SPV_Loader.Controllers
{
    public class AsasaraProcessBHNController : Controller
    {
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
                exportAsasara.jobQty = job.BuildQty.ToString();
                exportAsasara.integratorName = job.Integrator_Share;
                if (exportAsasara.integratorName == "BHN")
                {
                    exportAsasara.integratorName = "Blackhawk";
                }

                //activationType
                exportAsasara.activationType = ActivationType(asasaraViewModel);

                //Region
                using (SpvLoaderEntities context = new SpvLoaderEntities())
                {
                    var assasaraRegionLookup = context.AsasaraRegionLookups.ToList();
                    foreach (var region in assasaraRegionLookup)
                    {
                        if (country.Equals(region.Country))
                        {
                            exportAsasara.regionID = region.Region;
                        }
                    }
                }

                exportAsasara.pptQty = job.Test_Quantity_Production_Data_Proof.ToString();

                //Denomination
                string denomination = job.Denomination.Substring(job.Denomination.IndexOf(':') + 2);
                exportAsasara.denomination = denomination;

                exportAsasara.currency = Currency.GetCurrency(job.Country.ToString());

                //Retail Barcode
                exportAsasara.retailBarcode = job.Production_UPC.ToString();// Retail Barcode
                if (exportAsasara.retailBarcode != "")
                {
                    string retailBarcode = Regex.Replace(exportAsasara.retailBarcode.ToString(), "[^0-9]", "");
                    exportAsasara.retailBarcode = retailBarcode;
                }

                exportAsasara.customerJobNumber = asasaraViewModel.AsasaraDetails.PartNumber;
                exportAsasara.integratorID = asasaraViewModel.AsasaraDetails.Integrator_Product_ID;

                //Product Description
                exportAsasara.productDescription = job.Identifier.ToString();
                int indexOfFirstDash = exportAsasara.productDescription.IndexOf('-');
                exportAsasara.productDescription = exportAsasara.productDescription.Substring(indexOfFirstDash + 1);// remove first dash 
                exportAsasara.productDescription = exportAsasara.productDescription.Substring(exportAsasara.productDescription.IndexOf('-') + 2);// get chars after second dash
                exportAsasara.maskID = "MASK01_BLACKHAWKGG";
                exportAsasara.dummyRecords = "False";

                exportAsasara.maskID = "MASK01_BLACKHAWKGG";
                exportAsasara.dummyRecords = "False";
                exportAsasara.dummyRecordsCount = "0";
                exportAsasara.dummyRecordsEvery = "0";
                exportAsasara.DODHumanFontTypeID = "Yes";
                exportAsasara.packQty = job.Cards_Per_Pack.ToString();
                exportAsasara.caseQty = job.Cards_Per_Carton.ToString();
                exportAsasara.palletQty = "44800";
                exportAsasara.expiryDate = "4912";
                exportAsasara.whiteCardTestQty = job.Test_Quantity_Sandbox_Dev.ToString();

                //Retail Barcode Type
                if (exportAsasara.retailBarcode.Length == 12)
                {
                    exportAsasara.retailBarcodeType = "UPC";
                }
                else if (exportAsasara.retailBarcode.Length == 13)
                {
                    exportAsasara.retailBarcodeType = "EAN";
                }

                //Retail Barcode Style Type
                if (exportAsasara.activationType.ToUpper().Contains("UCC/EAN128") || exportAsasara.activationType.ToUpper().Contains("GS1-128"))
                {
                    exportAsasara.barcodeStyleType = "GS1-128";
                }
                else if (exportAsasara.activationType.ToUpper().Contains("C128"))
                {
                    exportAsasara.barcodeStyleType = "Code 128C";
                }

                exportAsasara.alternativePartNumber = job.NAN___Shipping_PO___Product_Packing_Label.ToString();
                exportAsasara.country = job.Country.ToString();

                //EAN Bundle
                string bundle = job.Pack_EAN.ToString();
                bundle = Regex.Replace(bundle, "[^0-9.]", "");
                exportAsasara.eanBundle = bundle;

                //EAN Box
                string box = job.Case_EAN.ToString();
                box = Regex.Replace(box, "[^0-9.]", "");
                exportAsasara.eanBox = box;

                string pallet = job.Pallet_EAN.ToString();
                pallet = Regex.Replace(pallet, "[^0-9.]", "");
                exportAsasara.eanPallet = pallet;

                exportAsasara.DODHumanFontTypeID = "Yes";
                exportAsasara.partnerTextBox = "Google Play";

                //Partner Code
                if (job.MMYY != null)
                {
                    var mmyy = job.MMYY;
                    string result = mmyy.Substring(0, 2) + "/" + mmyy.Substring(2);
                    exportAsasara.partnerCode = result;
                }

                exportAsasara.palletTypeID = "EURO";

                //Recipient Address
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
                else { exportAsasara.recipientAddress5 = ""; }

                if (c >= 6) { exportAsasara.recipientAddress6 = addressParts[5].Trim(); }
                else { exportAsasara.recipientAddress6 = ""; }

                int fai1 = (int)job.Google_FAI_Quantity;
                int fai2 = (int)job.Integrator_FAI_Quantity;
                int fai = fai1 + fai2;
                exportAsasara.FAIQty = fai.ToString();

                exportAsasara.OCR = job.OCR.ToString();

                //Label Style
                if (country == "DE" || country == "AT" || country == "CH")
                {
                    exportAsasara.labelStyle = "Blackhawk Dach";
                }
                else
                {
                    exportAsasara.labelStyle = "Blackhawk Generic";
                }

                //Activation Mode
                string activationMode = job.Internal_Activation.ToString();
                if (activationMode.ToUpper() == "BARCODE+MAGSTRIPE")
                {
                    exportAsasara.activationMode = "Hybrid";
                }
                if (activationMode.ToUpper() == "BARCODE")
                {
                    exportAsasara.activationMode = "Barcode Only";
                }
                if (activationMode.ToUpper() == "MAGSTRIPE")
                {
                    exportAsasara.activationMode = "Magstripe Only";
                }

                // pinFileLabel File random code generation
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

                //BHN PO Number
                exportAsasara.BHNPONumber = job.NAN___Shipping_PO___Product_Packing_Label.ToString();
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





















//string activation = job.Internal_Activation.ToString();
//if (activation.ToUpper() == "BARCODE+MAGSTRIPE" || activation.ToUpper() == "BARCODE + MAGSTRIPE")
//{
//    activation = "Hybrid";
//}

//if (country == "ES" || country == "AT" || country == "FR" || country == "DE" || country == "CH" || country == "PL" || country == "ZA")
//{
//    exportAsasara.activationType = integrator + " " + activation + " C128";
//}
//else if (country == "UK" || country == "NI" || country == "GB" || country == "IE" || country == "ROI" || country == "BE" || country == "NL" || country == "SE" || country == "DK" || country == "NO" || country == "FI")
//{
//    exportAsasara.activationType = integrator + " " + activation + " UCC/EAN128";
//}
//else if (country == "IT")
//{
//    exportAsasara.activationType = "BHN GS1-128 34/37 digits";
//}
//else
//{
//    exportAsasara.activationType = "Country Invalid";
//}









//if (integrator.ToUpper() == "BHN")
//{
//    AsasaraJob blackhawkJob = d.GetJob(integratorID); // generate Epay job line object from Job class in DAL

//    activationTypeTextBox.ForeColor = System.Drawing.Color.Black; // reset if previous line item threw the new activation type error
//    activationTypeTextBox.Font.Bold = false;

//    customerJobNumberTextBox.Text = blackhawkJob.PartNumber.ToString();
//    jobIDTextBox.Text = blackhawkJob.JobNumber.ToString();
//    orderIDTextBox.Text = blackhawkJob.SalesOrder.ToString();
//    tbxCustomerAccountCode.Text = blackhawkJob.CustomerAccountCode.ToString();
//    integratorNameTextBox.Text = blackhawkJob.Integrator_Share.ToString();
//    if (integratorNameTextBox.Text == "BHN")
//    {
//        integratorNameTextBox.Text = "Blackhawk";
//    }
//    maskIDTextBox.Text = "MASK01_BLACKHAWKGG";
//    dummyRecordsTextBox.Text = "False";
//    dummyRecordsCountTextBox.Text = "0";
//    dummyRecordsEveryTextBox.Text = "0";
//    DODHumanFontTypeIDComboBox.Text = "Yes";
//    palletTypeIDComboBox.Text = "EURO";
//    denominationTextBox.Text = blackhawkJob.Denomination.Substring(blackhawkJob.Denomination.IndexOf(':') + 2);
//    if (denominationTextBox.Text.Contains("-"))
//    {
//        denominationTextBox.Text = "0";
//    }
//    currencyTextBox.Text = Currency.GetCurrency(blackhawkJob.Country.ToString());

//    retailBarcodeTextBox.Text = blackhawkJob.Production_UPC.ToString();// Retail Barcode
//    if (retailBarcodeTextBox.Text != "")
//    {
//        string retailBarcode = Regex.Replace(retailBarcodeTextBox.Text.ToString(), "[^0-9]", "");
//        retailBarcodeTextBox.Text = retailBarcode;
//    }
//    productDescriptionTextBox.Text = blackhawkJob.Identifier.ToString();
//    int indexOfFirstDash = productDescriptionTextBox.Text.IndexOf('-');
//    productDescriptionTextBox.Text = productDescriptionTextBox.Text.Substring(indexOfFirstDash + 1);// remove first dash 
//    productDescriptionTextBox.Text = productDescriptionTextBox.Text.Substring(productDescriptionTextBox.Text.IndexOf('-') + 2);// get chars after second dash
//    packQtyTextBox.Text = blackhawkJob.Cards_Per_Pack.ToString();
//    caseQtyTextBox.Text = blackhawkJob.Cards_Per_Carton.ToString();
//    //palletQtyTextBox.Text = "29000";
//    //palletQtyTextBox.Text = "46200";
//    palletQtyTextBox.Text = "44800";
//    expiryDateTextBox.Text = "4912";
//    alternativePartNumberTextBox.Text = blackhawkJob.NAN___Shipping_PO___Product_Packing_Label.ToString();
//    countryTextBox.Text = blackhawkJob.Country.ToString();

//    string bundle = blackhawkJob.Pack_EAN.ToString();
//    bundle = Regex.Replace(bundle, "[^0-9.]", "");
//    eanBundleTextBox.Text = bundle;

//    string box = blackhawkJob.Case_EAN.ToString();
//    box = Regex.Replace(box, "[^0-9.]", "");
//    eanBoxTextBox.Text = box;

//    string pallet = blackhawkJob.Pallet_EAN.ToString();
//    pallet = Regex.Replace(pallet, "[^0-9.]", "");
//    eanPalletTextBox.Text = pallet;

//    BHNPONumberTextBox.Text = blackhawkJob.NAN___Shipping_PO___Product_Packing_Label.ToString();

//    string s = blackhawkJob.Ship_To_Location_Text.ToString(); // Ship To Address
//    string[] addressParts = s.Split(',');
//    int c = addressParts.Count();
//    if (c >= 1) { recipientAddress1TextBox.Text = addressParts[0].Trim(); }
//    else { recipientAddress1TextBox.Text = ""; }

//    if (c >= 2) { recipientAddress2TextBox.Text = addressParts[1].Trim(); }
//    else { recipientAddress2TextBox.Text = ""; }

//    if (c >= 3) { recipientAddress3TextBox.Text = addressParts[2].Trim(); }
//    else { recipientAddress3TextBox.Text = ""; }

//    if (c >= 4) { recipientAddress4TextBox.Text = addressParts[3].Trim(); }
//    else { recipientAddress4TextBox.Text = ""; }

//    if (c >= 5) { recipientAddress5TextBox.Text = addressParts[4].Trim(); }
//    else { recipientAddress5TextBox.Text = ""; }

//    if (c >= 6) { recipientAddress6TextBox.Text = addressParts[5].Trim(); }
//    else { recipientAddress6TextBox.Text = ""; }

//    jobQtyTextBox.Text = (blackhawkJob.BuildQty - blackhawkJob.Test_Quantity_Sandbox_Dev).ToString();

//    //activationTypeTextBox
//    string country = blackhawkJob.Country.ToString().ToUpper();
//    string activation = blackhawkJob.Internal_Activation.ToString();

//    if (activation.ToUpper() == "BARCODE+MAGSTRIPE" || activation.ToUpper() == "BARCODE + MAGSTRIPE")
//    {
//        activation = "Hybrid";
//    }

//    if (country == "ES" || country == "AT" || country == "FR" || country == "DE" || country == "CH" || country == "PL" || country == "ZA")
//    {
//        activationTypeTextBox.Text = integrator + " " + activation + " C128";
//    }
//    else if (country == "UK" || country == "NI" || country == "GB" || country == "IE" || country == "ROI" || country == "BE" || country == "NL" || country == "SE" || country == "DK" || country == "NO" || country == "FI")
//    {
//        activationTypeTextBox.Text = integrator + " " + activation + " UCC/EAN128";
//    }
//    else if (country == "IT")
//    {
//        activationTypeTextBox.Text = "BHN GS1-128 34/37 digits";
//    }
//    else
//    {
//        string myStringVariable = "Please check Country Code " + country + " and if new then add to AsasaraRegionLookup table and make sure Activation Type Country Code is included at Line 745 of code";
//        ClientScript.RegisterStartupScript(this.GetType(), "myalert", "alert('" + myStringVariable + "');", true);
//    }

//    using (SqlConnection con = new SqlConnection(connectStr))
//    {
//        using (SqlCommand cmd = new SqlCommand("select * from AsasaraRegionLookup", con))
//        {
//            con.Open();
//            SqlDataAdapter da = new SqlDataAdapter(cmd);
//            DataTable RegionLookup = new DataTable();
//            da.Fill(RegionLookup);
//            cmd.ExecuteNonQuery();

//            for (int i = 0; i < RegionLookup.Rows.Count; i++)
//            {
//                if (country.ToUpper() == RegionLookup.Rows[i][1].ToString())
//                {
//                    regionIDTextBox.Text = RegionLookup.Rows[i][2].ToString();
//                    break;
//                }
//            }
//        }
//        con.Close();
//    }

//    pptQtyTextBox.Text = blackhawkJob.Test_Quantity_Production_Data_Proof.ToString();
//    integratorIDTextBox.Text = blackhawkJob.Integrator_Product_ID.ToString();
//    whiteCardTestQtyTextBox.Text = blackhawkJob.Test_Quantity_Sandbox_Dev.ToString();

//    if (activationTypeTextBox.Text.ToUpper().Contains("UCC/EAN128") || activationTypeTextBox.Text.ToUpper().Contains("GS1-128"))
//    {
//        barcodeStyleTypeTextBox.Text = "GS1-128";
//    }
//    else if (activationTypeTextBox.Text.ToUpper().Contains("C128"))
//    {
//        barcodeStyleTypeTextBox.Text = "Code 128C";
//    }

//    int fai1 = (int)blackhawkJob.Google_FAI_Quantity;
//    int fai2 = (int)blackhawkJob.Integrator_FAI_Quantity;
//    int fai = fai1 + fai2;
//    FAIQtyTextBox.Text = fai.ToString();

//    OCRTextBox.Text = blackhawkJob.OCR.ToString();

//    if (country == "DE" || country == "AT" || country == "CH")
//    {
//        labelStyleTextBox.Text = "Blackhawk Dach";
//    }
//    else
//    {
//        labelStyleTextBox.Text = "Blackhawk Generic";
//    }

//    string activationMode = blackhawkJob.Internal_Activation.ToString();

//    if (activationMode.ToUpper() == "BARCODE+MAGSTRIPE")
//    {
//        activationModeTextBox.Text = "Hybrid";
//    }
//    if (activationMode.ToUpper() == "BARCODE")
//    {
//        activationModeTextBox.Text = "Barcode Only";
//    }
//    if (activationMode.ToUpper() == "MAGSTRIPE")
//    {
//        activationModeTextBox.Text = "Magstripe Only";
//    }

//    if (retailBarcodeTextBox.Text.Length == 12)
//    {
//        retailBarcodeTypeTextBox.Text = "UPC";
//    }
//    else if (retailBarcodeTextBox.Text.Length == 13)
//    {
//        retailBarcodeTypeTextBox.Text = "EAN";
//    }

//    partnerTextBox.Text = "Google Play";

//    brandTextBox.Visible = false;
//    lblBrand.Visible = false;
//    VASBoxDescriptionTextBox.Visible = false;
//    lblVasBoxDescription.Visible = false;
//    codeFormatTextBox.Visible = false;
//    lblCodeFormat.Visible = false;
//    vasTypeTextBox.Visible = false;
//    lblVasType.Visible = false;
//    VASPackDescriptionTextBox.Visible = false;
//    lblVasPackDescription.Visible = false;

//    // pinFileLabel File random code generation
//    pinFileTextBox.Text = RandomString(6);

//    // White Test Card Pin File - random code
//    if (blackhawkJob.Test_Quantity_Sandbox_Dev.ToString() != "")
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