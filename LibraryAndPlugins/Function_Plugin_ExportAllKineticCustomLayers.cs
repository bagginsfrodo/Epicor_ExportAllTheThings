/*
* ==========================================================================================
* AUTHOR:    Kevin Barrow
* COPYRIGHT: Kevin Barrow 2026
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllKineticCustomLayers
* Description: This plugin downloads all custom Kinetic layers (Base Apps).
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
* 09/04/2024 | klincecum | Kevin Lincecum | Initial Implementation
* 01/21/2026 | kbarrow   | Kevin Barrow   | Refactored to use Core_ExportKineticMetaFX
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
        // Configuration for Custom Base Apps
        var config = new 
        {
            ExportBaseApps = true,
            ExportLayers = false,
            SystemFlag = false
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