/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllFunctionLibraries
* Description: This plugin downloads all Function Libraries.
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

  //Can't make this damn thing directly, wtf?
  Func<(int kind, string startsWith, int rollOutMode, int status), Ice.Lib.EfxLibraryDesigner.LibrarySearchOptions> CreateLibrarySearchOptions = (input) =>
  {
      return JsonConvert.DeserializeObject<Ice.Lib.EfxLibraryDesigner.LibrarySearchOptions>( JsonConvert.SerializeObject(new {input.kind, input.startsWith, input.rollOutMode, input.status}) );
  };


  try
  {
  //****
  
     CallService<Ice.Contracts.EfxLibraryDesignerSvcContract>(efxLD =>
     {
        //Files we will be adding to the zip file     
        Dictionary<string, string> mainZipFileDictionary = new Dictionary<string, string>(); 
     
        //Get all Function Libraries
        var librarySearchOptions = CreateLibrarySearchOptions((kind: 1, startsWith: "", rollOutMode: 2, status: 2));
    
        EfxLibrarySearchTableset libraries = efxLD.GetLibraryList(librarySearchOptions);
        
        EfxLibraryTableset defaults = efxLD.GetDefaults();
        
        System.Version version = new System.Version($"{defaults.EfxLibrary.FirstOrDefault().EpicorVersion}.{defaults.EfxLibrary.FirstOrDefault().Revision}");
        
        //Loop through the list.
        foreach(var item in libraries.EfxLibraryList)
        {
            try
            {
                //Export the Library and get the binary data.
                var exportOptions = new Ice.Lib.EfxLibraryDesigner.ExportOptions()
                {
                    Mode = 0, Format = (Ice.Lib.EfxLibraryDesigner.ExportFileFormat)1, Package = null, PackageVersion = version, Publisher = Session.UserID, InstallAsHidden = false
                };

                byte[] itemBytes = efxLD.ExportLibrary(item.LibraryID, exportOptions);
                
                //Convert the data to a Base64 encoded string.
                string itemBase64 = Convert.ToBase64String(itemBytes);
                
                //Add the data as a file in the zip.
                mainZipFileDictionary.Add($"{item.LibraryID}.efxj", itemBase64);

            }
            catch (Exception iEx)
            {
                //Continue processing on error, but add errors to the errors list.
                iEx.Data.Add("LibraryID", item.LibraryID);
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