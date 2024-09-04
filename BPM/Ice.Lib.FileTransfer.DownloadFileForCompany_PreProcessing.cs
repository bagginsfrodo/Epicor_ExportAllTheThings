/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Directive Type:  Method -> Pre Processing
* Directive BO:    Ice.Lib.FileTransfer.DownloadFileForCompany
* Directive Name:  InterceptFileDownloadForCompanyRequest
* Directive Desc:  Intercepts the call to download a file if a custom url is used, and routes
*                  it appropriately.
* Directive Group: ExportAllTheThings
* ==========================================================================================
* 
* Hi Mom!
*
* CHANGELOG:
* 08/26/2024 | klincecum | Kevin Lincecum | Initial Implementation
* 08/30/2024 | klincecum | Kevin Lincecum | Cleanup & Comments For Release
*
* ==========================================================================================
*/

//We are only interested in requests that the server path starts with "EATT://"
if(serverPath.StartsWith("EATT://"))
{
    try
    {
        //Get the plugin name
        string plugin = serverPath.Split("//")[1];
        
        //Call the plugin (function)
        var response = InvokeFunction("ExportAllTheThings", plugin);
        
        //So y'all can see what was returned better
        var responseUnwrapped = new
        {
            Success = (bool)response[0],
            ListErrorJson = (string)response[1],
            ZipBase64 = (string)response[2]
        };
    
    
        //If we succeeded, we are done. Convert the Base64 Data from the plugin (function) to a byte array and return it. 
        if(responseUnwrapped.Success) //Success
        {
            if(!String.IsNullOrEmpty(responseUnwrapped.ZipBase64))
            {
              result = Convert.FromBase64String(responseUnwrapped.ZipBase64);
              MarkCallCompleted();
            }
            
            return; //Done. Stop Processing.
        }
        
        //Failure
        
        //Create an exception
        var exception = new BLException( "Failure running plugin with path: \"" + serverPath + "\"" );
        
        if(!String.IsNullOrEmpty(responseUnwrapped.ListErrorJson))
        {
            //If we have errors, pass them along with the exception.
            exception.Data.Add("ErrorList", responseUnwrapped.ListErrorJson);
        }
        
        //Bam!
        throw exception;
            
    }    
    catch (Exception ex)
    {
        //We will just rethrow anything. Catches our exceptions above, as well as unknowns.
        throw new BLException(ex.ToString());
    }
}