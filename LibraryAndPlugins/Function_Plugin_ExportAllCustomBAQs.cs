/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllCustomBAQs
* Description: This plugin downloads all custom BAQs.
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
* 08/26/2024 | klincecum | Kevin Lincecum | Initial Implementation
* 08/30/2024 | klincecum | Kevin Lincecum | Cleanup & Comments For Release
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
  
     CallService<Ice.Contracts.BAQDesignerSvcContract>(baqD =>
     {
     
        //Files we will be adding to the zip file 
        Dictionary<string, string> mainZipFileDictionary = new Dictionary<string, string>();
     
        //Get all BAQs that are not marked "System".
        bool more = false;
        BAQDesignerListTableset baqListTS = baqD.GetList("SystemFlag=false", 0, 1, out more);
        
        //Loop through the list.
        foreach(DynamicQueryDesignerListRow item in baqListTS.DynamicQueryDesignerList)
        {
            try
            {
                //Export the BAQ and get the binary data.
                Dictionary<string, string> options = new Dictionary<string, string>();
                List<string> logResult = new List<string>(); //Needed for export call.
                
                byte[] itemBytes = baqD.ExportBaq(item.QueryID, ref options, out logResult);
                
                //Convert the data to a Base64 encoded string.
                string itemBase64 = Convert.ToBase64String(itemBytes);
                
                //Add the data as a file in the zip.
                mainZipFileDictionary.Add($"{item.QueryID}.baq", itemBase64);
            }
            catch (Exception iEx)
            {
                //Continue processing on error, but add errors to the errors list.
                iEx.Data.Add("QueryID", item.QueryID);
                ListErrorJson = AddExceptionToList(iEx, ListErrorJson);
            }            
        }
        
        //Zip the files
        string fileDictionaryJson = JsonConvert.SerializeObject(mainZipFileDictionary);
        
        //Return the zip file data as a Base64 encoded string.
        ZipBase64 = ThisLib.ZipFiles(fileDictionaryJson);
     
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