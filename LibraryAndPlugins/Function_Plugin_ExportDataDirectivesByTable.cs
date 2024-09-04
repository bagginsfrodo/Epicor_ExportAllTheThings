/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportDataDirectivesByTable
* Description: This plugin downloads all Data Directives by "Table".
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
  
     CallService<Ice.Contracts.BpMethodSvcContract>(bpMethod =>
     {
        //Files we will be adding to the zip file     
        Dictionary<string, string> mainZipFileDictionary = new Dictionary<string, string>(); 
     
        //Get All Data Directives
        bool morePages = false;
        Ice.Tablesets.BpMethodListTableset methodTS = bpMethod.GetList("Source = 'DB'", 0, 0, out morePages);
        
        //Create a Distinct List of Data Directive Methods
        var methodsList = methodTS.BpMethodList.Select(x => new { x.SystemCode, x.BusinessObject }).ToList().Distinct();
        
        //Loop through the list.
        foreach(var item in methodsList)
        {
            try
            {
                 //Export the Data Directive
                 byte[] itemBytes = bpMethod.ExportByTable(item.SystemCode, item.BusinessObject).Data;
                      
                 //Convert the data to a Base64 encoded string.           
                 string itemBase64 = Convert.ToBase64String(itemBytes);
                 
                 //Add the data as a file in the zip.           
                 mainZipFileDictionary.Add($"{item.SystemCode}.{item.BusinessObject}.bpm", itemBase64);
            }
            catch (Exception iEx)
            {
                //Continue processing on error, but add errors to the errors list.
                iEx.Data.Add("Table", $"{item.SystemCode}.{item.BusinessObject}.bpm");
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