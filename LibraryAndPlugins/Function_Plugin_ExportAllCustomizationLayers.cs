/*
* ==========================================================================================
* AUTHOR:    Kevin Barrow
* COPYRIGHT: Kevin Barrow 2026
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllKineticCustomizationLayers
* Description: This plugin downloads all Kinetic Customization Layers.
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
* 01/21/2026 | kbarrow   | Kevin Barrow   | Initial Implementation via Core_ExportKineticMetaFX
*
* ==========================================================================================
*/
  //Helper Functions Section----------------------------------------------------------------------------------------------------------------------------------------->
  Func<Exception, string, string> AddExceptionToList = (exception, exceptionListJson) =>
  {
      List<Exception> exceptionList = new List<Exception>(){exception};
      if(!String.IsNullOrEmpty(exceptionListJson)) { try { exceptionList.AddRange( JsonConvert.DeserializeObject<List<Exception>>(exceptionListJson) ); } catch {} }
      return JsonConvert.SerializeObject(exceptionList);
  };
  //<-----------------------------------------------------------------------------------------------------------------------------------------Helper Functions Section
 
 
  try
  {
  //****
  
     CallService<Ice.Contracts.MetaFXSvcContract>(metaFX =>
     {
        // Configuration for Customization Layers
        var config = new 
        {
            ExportBaseApps = false,
            ExportLayers = true,
            SystemFlag = false,
            LayerTypeCodes = new [] { "KNTCCustLayer" }
        };
        
        string configJson = JsonConvert.SerializeObject(config);
        
        // Call Centralized Core Function
        string resultJson = ThisLib.Core_ExportKineticMetaFX(configJson);
        
        dynamic result = JsonConvert.DeserializeObject(resultJson);
        Success = result.Success;
        ListErrorJson = result.ListErrorJson;
        ZipBase64 = result.ZipBase64;
     }); 
     
  //****   
  }
  catch (Exception ex)
  {
      Success = false;
      ListErrorJson = AddExceptionToList(ex, ListErrorJson);
  }
  finally
  {
      //Maybe later?
  }