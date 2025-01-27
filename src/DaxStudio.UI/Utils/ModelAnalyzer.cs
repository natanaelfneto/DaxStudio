﻿using ADOTabular;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.AnalysisServices;
using DaxStudio.UI.Extensions;
using System.IO.Packaging;
using System.IO;
using Newtonsoft.Json;
using System.Data.OleDb;

namespace DaxStudio.UI.Utils
{

    public static class ModelAnalyzer
    {
      
        public static void ExportVPAX(string serverName, string databaseName, string path, bool includeTomModel, string applicationName, string applicationVersion)
        {
            //
            // Get Dax.Model object from the SSAS engine
            //
            Dax.Model.Model model = Dax.Model.Extractor.TomExtractor.GetDaxModel(serverName, databaseName, applicationName, applicationVersion);

            //
            // Get TOM model from the SSAS engine
            //
            Microsoft.AnalysisServices.Database database = includeTomModel ? Dax.Model.Extractor.TomExtractor.GetDatabase(serverName, databaseName): null;

            // 
            // Create VertiPaq Analyzer views
            //
            Dax.ViewVpaExport.Model viewVpa = new Dax.ViewVpaExport.Model(model);

            //
            // Save VPAX file
            // 
            // TODO: export of database should be optional
            Dax.Vpax.Tools.VpaxTools.ExportVpax(path, model, viewVpa, database);
        }
        
    }
}
