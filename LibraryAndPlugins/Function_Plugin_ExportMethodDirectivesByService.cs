/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportMethodDirectivesByService
* Description: This plugin downloads all Method Directives by "Service".
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
     
        //Get all Directives for a "Service". (Also exclude BAQ BPMs)
        bool morePages = false;
        Ice.Tablesets.BpMethodListTableset methodTS = bpMethod.GetList("Source = 'BO' AND ObjectNS <> ''", 0, 0, out morePages);
        
        //Create a unique list to process.
        var methodsList = methodTS.BpMethodList.Select(x => new { x.SystemCode, x.ObjectNS, x.BusinessObject }).ToList().Distinct();
        
        //Loop through the list.
        foreach(var item in methodsList)
        {
            try
            {
                //Export the directive and get the binary data.
                byte[] zipMethodBytes = bpMethod.ExportByService(item.SystemCode, item.ObjectNS, item.BusinessObject).Data;
                
                //Convert the data to a Base64 encoded string.                
                string methodZipBase64 = Convert.ToBase64String(zipMethodBytes);

                //Add the data as a file in the zip.
                mainZipFileDictionary.Add($"{item.SystemCode}.{item.ObjectNS}.{item.BusinessObject}.bpm", methodZipBase64);
            }
            catch (Exception iEx)
            {
                //Continue processing on error, but add errors to the errors list.
                iEx.Data.Add("Service", $"{item.SystemCode}.{item.ObjectNS}.{item.BusinessObject}");
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