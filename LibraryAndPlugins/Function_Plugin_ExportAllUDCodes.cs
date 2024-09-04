/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ExportAllUDCodes
* Description: This plugin exports all UD Codes. (User Codes)
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
  
  //Serialize a whole Tableset to B64 
  Func<IceTableset, string> GetTablesetBytesAsB64 = (ts) =>
  {
      string tempJson = JsonConvert.SerializeObject(ts, Formatting.Indented);
        
      byte[] tempBytes = System.Text.Encoding.UTF8.GetBytes(tempJson);
        
      return Convert.ToBase64String(tempBytes);
  };
 
 
  try
  {
  //****
  
     CallService<Ice.Contracts.UserCodesSvcContract>(udCode =>
     {
     
        //Files we will be adding to the zip file     
        Dictionary<string, string> mainZipFileDictionary = new Dictionary<string, string>();

        //Get All UD Codes
        bool morePages = false;
        UserCodesTableset udCodeTS = udCode.GetRows("", "", 0, 0, out morePages);
      
        //Add all to zip file
        mainZipFileDictionary.Add("AllCodes.json", GetTablesetBytesAsB64(udCodeTS));

      
        //Get Distinct List of UDCode Types
        var udCodeTypes = udCodeTS.UDCodeType.Select(x => x.CodeTypeID).Distinct().ToList();
        
        
        //Add Individual codes by CodeTypeID to zip file        
        foreach(var codeType in udCodeTypes)
        {
            //Need something to dump it into
            UserCodesTableset tempUDCodeTS = new UserCodesTableset();
            
            //Filter to the individual CodeTypeID and cheat using Newtonsoft to do the copy
            UDCodesTable filteredUDCodesTable = JsonConvert.DeserializeObject<UDCodesTable>(JsonConvert.SerializeObject(udCodeTS.UDCodes.Where(x => x.CodeTypeID == codeType)));
            UDCodeTypeTable filteredUDCodeTypeTable = JsonConvert.DeserializeObject<UDCodeTypeTable>(JsonConvert.SerializeObject(udCodeTS.UDCodeType.Where(x => x.CodeTypeID == codeType)));
            
            //Add the ranges back to the TS
            tempUDCodeTS.UDCodes.AddRange(filteredUDCodesTable);
            tempUDCodeTS.UDCodeType.AddRange(filteredUDCodeTypeTable);
            
            //Zip the files
            Dictionary<string, string> perCodeTypeFileDictionary = new Dictionary<string, string>();
            
            perCodeTypeFileDictionary.Add($"{codeType}.json", GetTablesetBytesAsB64(tempUDCodeTS));
            
            string perCodefileDictionaryJson = JsonConvert.SerializeObject(perCodeTypeFileDictionary);
        
            string perCodeZipBase64 = ThisLib.ZipFiles(perCodefileDictionaryJson);
            
            //Add to the MAIN zip file
            mainZipFileDictionary.Add($"{codeType}.zip", perCodeZipBase64);
        }

        //Zip the files (The main one)
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