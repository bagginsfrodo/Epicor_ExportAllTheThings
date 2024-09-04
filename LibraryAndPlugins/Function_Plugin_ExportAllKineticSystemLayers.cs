/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllKineticSystemLayers
* Description: This plugin downloads all system Kinetic layers.
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
          //Create a request to list the apps       
          var request = new Epicor.MetaFX.Core.Models.Applications.ApplicationRequest()
          {
              Type = "view",
              SubType = "",
              SearchText = "",
              IncludeAllLayers = true
          };
  
          //Get a list of apps
          List<Epicor.MetaFX.Core.Models.Applications.Application> applications = metaFX.GetApplications(request);
          
          //Create an export request list 
          List<Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication> applicationList = new List<Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication>();
  
          //Loop through the list and add custom apps to the export list
          foreach(var item in applications.Where(x => x.SystemFlag == true))
          {
              applicationList.Add(new Epicor.MetaFX.Core.Models.Layers.EpMetaFxLayerForApplication() { Id = item.Id });
          }
          
          //Export the apps and return the zip file data as a Base64 encoded string.
          ZipBase64 = metaFX.ExportLayers(applicationList);
       }); 
       
       Success = true;
       
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