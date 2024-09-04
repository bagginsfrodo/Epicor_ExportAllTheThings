/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Directive Type:  Method -> Post Processing
* Directive BO:    Ice.Lib.MetaFX.GetApp
* Directive Name:  InterceptGetAppRequest
* Directive Desc:  Intercepts a request for a template, forward it to the appropriate place.
* Directive Group: ExportAllTheThings
* ==========================================================================================
* 
* Hi Mom!
*
* CHANGELOG:
* 08/26/2024 | klincecum | Kevin Lincecum | Initial Implementation
* 08/29/2024 | klincecum | Kevin Lincecum | Cleanup & Comments For Release
*
* ==========================================================================================
*/

//If we are in app studio, we only want to edit the template, not a dynamically generated app.
if(request.properties.mode == "AppStudio") return; //Bye

if(request.id == "Ice.UI.ExportAllTheThingsTemplate")
{
    //Where we are going to call
    string FunctionLibrary = "ExportAllTheThings";
    string FunctionID = "GetApp";

    try
    {
      //Call the function with the request and template data. This is one of the reasons we are doing this in post so the system will retrieve it for us.
      var response = InvokeFunction(FunctionLibrary, FunctionID,  JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(result));
 
       
      //So y'all can see what was returned better
      var responseUnwrapped = new
      {
          Success = (bool)response[0],
          ListErrorJson = (string)response[1],
          appJson = (string)response[2]
      };
      
      //If we succeeded, we are done. Parse the Json into tokens and pass it back. The serializer will take care of it from here.
      if(responseUnwrapped.Success) //Success
      {
          result = JToken.Parse(responseUnwrapped.appJson);
          return; //Done. Stop Processing.
      }
      
      
      //Failure
      if(!String.IsNullOrEmpty(responseUnwrapped.ListErrorJson))
      {
          //If we have errors, pass them along with the exception.
          throw new BLException(responseUnwrapped.ListErrorJson);
      }
      else
      {
          //We didn't have any errors, so we'll pass back "something" useful lol.
          throw new BLException($"Unknown error from function: {FunctionLibrary}.{FunctionID}");
      }

    }
    catch (Exception ex)
    {
        //We will just rethrow anything. Catches our exceptions above, as well as unknowns.
        throw ex;
    }
}