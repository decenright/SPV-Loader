using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SPV_Loader.Models
{
    public class AthenaViewModel
    {
        public IEnumerable<AthenaJob> AthenaList { get; set; }

        public ExportAthena ExportAthena { get; set; }

        public AthenaJob AthenaDetails { get; set; }

        public BHNWorkInstruction BlackhawkModel { get; set; }

        public bool IsDach { get; set; }

        [Required(ErrorMessage = "Dach Country is required.")]
        public string DachCountry { get; set; }

        public SelectList DachCountryList { get; set; }

        public string DachDescription { get; set; }

        public DLCModel DLCModel { get; set; }
    }
}