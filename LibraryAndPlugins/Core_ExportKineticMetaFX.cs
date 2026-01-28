/*
* ==========================================================================================
* AUTHOR:    Kevin Barrow
* COPYRIGHT: Kevin Barrow 2026
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllKineticPersonalizationLayers
* Description: This plugin downloads all Kinetic Personalization Layers.
* ==========================================================================================
* 
* INPUTS: NONE
*
* OUTPUTS:
*   BOOL:   Success        -> Function Success / Failure
*   STRING: ListErrorsJson -> Json Serialized List<Exception>
*   STRING: ZipBase64      -> Base64 Encoded Byte Array
*
* CHANGELOG:
* 01/21/2026 | kbarrow | Initial Implementation via Core_ExportKineticMetaFX
* 01/28/2026 | kbarrow | BPM-safe normalize + single-pass + no member declarations
* ==========================================================================================
*/

// --- Helpers as delegates (BPM-safe: no method declarations) ---
Func<object, string> ToBase64 = exportResult =>
{
    if (exportResult == null) return null;

    var bytes = exportResult as byte[];
    if (bytes != null && bytes.Length > 0)
        return Convert.ToBase64String(bytes);

    var s = exportResult as string;
    if (!string.IsNullOrWhiteSpace(s))
    {
        // If it looks like a file path, try to read bytes
        if (s.IndexOfAny(new[] {'\\','/'}) >= 0)
        {
            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(s);
                return Convert.ToBase64String(fileBytes);
            }
            catch
            {
                // If file read fails, fall through and assume it's already base64
            }
        }
        return s;
    }
    return null;
};

Func<Exception, string, string> AddExceptionToList = (exception, exceptionListJson) =>
{
    List<Exception> exceptionList = new List<Exception>() { exception };
    if (!String.IsNullOrEmpty(exceptionListJson))
    {
        try
        {
            var prior = JsonConvert.DeserializeObject<List<Exception>>(exceptionListJson);
            if (prior != null) exceptionList.AddRange(prior);
        }
        catch
        {
            // ignore JSON issues
        }
    }
    return JsonConvert.SerializeObject(exceptionList);
};

// --- Outputs ---
string zipBase64 = "";
string listErrorJson = "";
bool success = false;

try
{
    // --- Input config (FIRST) ---
    dynamic config = JsonConvert.DeserializeObject(ConfigJson);
    bool exportBaseApps = config.ExportBaseApps ?? false;
    bool exportLayers   = config.ExportLayers ?? false;
    bool? systemFlag    = config.SystemFlag;
    JArray layerTypeCodesJ = config.LayerTypeCodes;
    List<string> layerTypeCodes = layerTypeCodesJ?.ToObject<List<string>>();
    string createdBy    = config.CreatedBy;
    string searchText   = config.SearchText;
    bool includePersLayers = config.IncludePersLayers ?? false;

    // Ensure personalization layers included if requested explicitly
    if (layerTypeCodes != null && layerTypeCodes.Contains("KNTCPersLayer"))
        includePersLayers = true;

    // If nothing to export, finish early
    if (!exportBaseApps && !exportLayers)
    {
        success = true;
        ResultJson = JsonConvert.SerializeObject(new { Success = success, ListErrorJson = listErrorJson, ZipBase64 = zipBase64 });
        return;
    }

    // --- Main service call (SINGLE PASS) ---
    CallService<Ice.Contracts.MetaFXSvcContract>(metaFX =>
    {
        var request = new Epicor.MetaFX.Core.Models.Applications.ApplicationRequest()
        {
            Type = "view",
            SubType = "",
            SearchText = searchText ?? "",
            IncludeAllLayers = true,
            IncludePersLayers = includePersLayers
        };

        var applications = metaFX.GetApplications(request);
        Dictionary<string, string> mainZipFileDictionary = new Dictionary<string, string>();

        foreach (dynamic app in applications)
        {
            // --- BASE APPS ---
            if (exportBaseApps)
            {
                bool match = true;
                if (systemFlag.HasValue) match = (app.SystemFlag == systemFlag.Value);
                if (match)
                {
                    try
                    {
                        var baseAppExport = new Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication
                        {
                            Id = app.Id.ToString(),
                            Company = Session.CompanyID,
                            DeviceType = "Desktop"
                        };

                        var list = new List<Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication> { baseAppExport };

                        var exportObj = metaFX.ExportLayers(list);
                        string base64 = ToBase64(exportObj);
                        if (!string.IsNullOrEmpty(base64))
                        {
                            string fileName = $"{app.Id.ToString()}.zip";
                            if (!mainZipFileDictionary.ContainsKey(fileName))
                                mainZipFileDictionary.Add(fileName, base64);
                        }
                    }
                    catch (Exception ex)
                    {
                        listErrorJson = AddExceptionToList(ex, listErrorJson);
                    }
                }
            }

            // --- LAYERS ---
            if (exportLayers && app.Layers != null)
            {
                foreach (dynamic layer in app.Layers)
                {
                    bool match = true;
                    if (systemFlag.HasValue) match = (layer.SystemFlag == systemFlag.Value);
                    if (match && layerTypeCodes != null && layerTypeCodes.Count > 0)
                        match = layerTypeCodes.Contains((string)layer.TypeCode);
                    if (match && !string.IsNullOrEmpty(createdBy))
                        match = (layer.LastUpdatedBy == createdBy || layer.CreatedBy == createdBy);

                    if (!match) continue;

                    try
                    {
                        var exportItem = new Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication
                        {
                            Id = layer.Id.ToString(),
                            LayerName = layer.LayerName.ToString(),
                            TypeCode = layer.TypeCode.ToString(),
                            Company = (layer.Company != null) ? layer.Company.ToString() : Session.CompanyID,
                            DeviceType = (layer.DeviceType != null) ? layer.DeviceType.ToString() : "Desktop",
                            IsPublished = true
                        };

                        var list = new List<Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication> { exportItem };

                        var exportObj2 = metaFX.ExportLayers(list);
                        string base64Data = ToBase64(exportObj2);

                        if (!string.IsNullOrEmpty(base64Data))
                        {
                            var safeLayerName = System.Text.RegularExpressions.Regex.Replace(
                                layer.LayerName.ToString(), @"[^a-zA-Z0-9_.\- ]", "_");
                            string fileName = $"{app.Id.ToString()}_{safeLayerName}.zip";
                            if (!mainZipFileDictionary.ContainsKey(fileName))
                                mainZipFileDictionary.Add(fileName, base64Data);
                        }
                    }
                    catch (Exception ex)
                    {
                        listErrorJson = AddExceptionToList(ex, listErrorJson);
                    }
                }
            }
        }

        if (mainZipFileDictionary.Count > 0)
        {
            string dictJson = JsonConvert.SerializeObject(mainZipFileDictionary);
            zipBase64 = ThisLib.ZipFiles(dictJson);
        }
    });

    success = true;
}
catch (Exception ex)
{
    success = false;
    listErrorJson = AddExceptionToList(ex, listErrorJson);
}

ResultJson = JsonConvert.SerializeObject(new { Success = success, ListErrorJson = listErrorJson, ZipBase64 = zipBase64 });
