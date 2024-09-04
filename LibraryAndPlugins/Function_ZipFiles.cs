/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    ZipFiles    
* Description: This is a utility function to "Zip" files.
* ==========================================================================================
* 
* INPUTS:
*   STRING: fileDictionaryJson -> Json Encoded Dictionary<string, string> where the first
*                                 string is the filename, and the second is a Base64
*                                 endoded byte array with the file data.
*
* OUTPUTS:
*   STRING: ZipBase64          -> Base64 Encoded Byte Array
*
* CHANGELOG:
* 08/26/2024 | klincecum | Kevin Lincecum | Initial Implementation
* 08/30/2024 | klincecum | Kevin Lincecum | Cleanup & Comments For Release
*
* ==========================================================================================
*/


  Func<Dictionary<string, string>, byte[]> ZipByteArray = (fileDict) =>
  {
      byte[] retBytes = null;
      
      using (MemoryStream zipMS = new MemoryStream())
      {
          using (ZipArchive zipArchive = new ZipArchive(zipMS, ZipArchiveMode.Create, true))
          {
              foreach(var file in fileDict)
              {
                  var zipArchiveEntry = zipArchive.CreateEntry(file.Key, CompressionLevel.NoCompression);
                  
                  using (var zipStream = zipArchiveEntry.Open())
                  {
                      byte[] fileBytes = Convert.FromBase64String(file.Value);
                  
                      zipStream.Write(fileBytes, 0, fileBytes.Length);
                  }
              }
          }
          
          zipMS.Flush();
          retBytes = zipMS.ToArray();
      };
      
      return retBytes;    
  };


  Dictionary<string, string> fileDictionary = new Dictionary<string, string>();
    
  try
  {
      fileDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileDictionaryJson); 
  }
  catch {}
  
  if(fileDictionary.Count() < 1) return; //Get out of here
  
  byte[] zipBytes = ZipByteArray(fileDictionary);

  ZipBase64 = Convert.ToBase64String(zipBytes);