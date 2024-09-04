/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportDirectivesByGroups
* Description: This plugin downloads all Method AND Data Directives by "Group".
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
        
        //Get All Directive Groups 
        List<string> groupList = bpMethod.GetDirectiveGroups("BO,DB").ToList();
        
        //Loop through the list.
        foreach(var item in groupList)
        {
            try
            {
                //Export the group and get the binary data.
                byte[] itemBytes = bpMethod.ExportByDirectiveGroup(item).Data;
                
                //Convert the data to a Base64 encoded string.                
                string itemBase64 = Convert.ToBase64String(itemBytes);
               
                //Create an appropriate file name.
                string filename = String.IsNullOrEmpty(item) ? "Ungrouped" : item;

                //Add the data as a file in the zip.           
                mainZipFileDictionary.Add($"{filename}.bpm", itemBase64);
            
            }
            catch (Exception iEx)
            {
                //Continue processing on error, but add errors to the errors list.
                iEx.Data.Add("Group", item);
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