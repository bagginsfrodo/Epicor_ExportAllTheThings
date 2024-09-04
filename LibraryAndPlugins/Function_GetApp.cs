/*
* ==========================================================================================
* AUTHOR:    Kevin Lincecum
* COPYRIGHT: Kevin Lincecum 2024
* LICENSE:   MIT
* ==========================================================================================
* Library:     ExportAllTheThings
* Function:    GetApp
* Description: This function parses a template app and builds a full app based off of the
*              plugins found in this library. These plugins export things as "Zip" Files.
* ==========================================================================================
* 
* INPUTS:
*   STRING: Request        -> The request for the app from the client   
*   STRING: TemplateData   -> The json data for the template app
*
* OUTPUTS:
*   BOOL:   Success        -> Function Success / Failure
*   STRING: ListErrorsJson -> Json Serialized List<Exception>
*   STRING: appJson        -> Kinetic app Json data
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
  
  
  //Use Newtonsoft to copy a token
  Func<JToken, JToken> DuplicateToken = (token) => JToken.Parse(token.ToString());


  try
  {
  //****

      //Create a dictionary of "plugins" to build the app with
      Dictionary<string, string> pluginDictionary = new Dictionary<string, string>();
    
      //Load the plugins into the dictionary
      CallService<Ice.Contracts.EfxLibraryDesignerSvcContract>(efxLD =>
      {
          //We are loading plugins from this libary, so get a list of the functions.
          var functionLibrary = efxLD.GetLibrary(LibraryID);
          
          //Populate the plugins dictionary. Only chose functions that begin with "Export".
          pluginDictionary = functionLibrary.EfxFunction.Where(f => f.FunctionID.StartsWith("Export")).ToDictionary(f1 => f1.FunctionID, f2 => f2.Description);
      });
    

      //Parse the template data.
      var app = JToken.Parse(TemplateData);
      
      //Get the body template section we are interested in. 
      var bodyComponentsTemplate = app["Layout"]["components"][0]["model"]["bodyComponents"];
      
      //Clear template data from the array.
      app["Layout"]["components"][0]["model"]["bodyComponents"] = new JArray();
      
      
      //Get the event template section we are interested in.
      var eventTemplate = app["Events"][0];
      
      //Clear template data from the array.
      app["Events"] = new JArray();
      
      
      //We are doing rows down the page. Get that section.
      var rowTemplate = bodyComponentsTemplate[0];
      
      //We will build a new row for each plugin.
      foreach(var func in pluginDictionary)
      {
          var newRow = DuplicateToken(rowTemplate);
      
          //Row
          newRow["id"] = Guid.NewGuid();
          newRow["model"]["guid"] = newRow["id"];
          newRow["model"]["id"] = $"row{func.Key}";
          newRow["model"]["labelText"] = func.Key;

          //Left Column Components
          newRow["components"][0]["id"] = Guid.NewGuid();
          newRow["components"][0]["model"]["guid"] = newRow["components"][0]["id"];
          newRow["components"][0]["model"]["id"] = $"lc{func.Key}";
          newRow["components"][0]["parentId"] = newRow["id"];
      
          //Button
          newRow["components"][0]["components"][0]["id"] = Guid.NewGuid();
          newRow["components"][0]["components"][0]["model"]["guid"] = newRow["components"][0]["components"][0]["id"];
          newRow["components"][0]["components"][0]["model"]["id"] = $"btn{func.Key}";
          newRow["components"][0]["components"][0]["parentId"] = newRow["components"][0]["components"][0]["id"];
      
          //Right Column Components
          newRow["components"][1]["id"] = Guid.NewGuid();
          newRow["components"][1]["model"]["guid"] = newRow["components"][1]["id"];
          newRow["components"][1]["model"]["id"] = $"rc{func.Key}";
          newRow["components"][1]["parentId"] = newRow["id"];
      
          //Plugin Description
          newRow["components"][1]["components"][0]["id"] = Guid.NewGuid();
          newRow["components"][1]["components"][0]["model"]["guid"] = newRow["components"][1]["components"][0]["id"];
          newRow["components"][1]["components"][0]["model"]["id"] = $"txt{func.Key}Description";
          newRow["components"][1]["components"][0]["model"]["labelText"] = func.Value;
          newRow["components"][1]["components"][0]["parentId"] = newRow["components"][1]["components"][0]["id"];
      
      
          //Add row to app.
          ((JArray)app["Layout"]["components"][0]["model"]["bodyComponents"]).Add(newRow);
          
      
          //Events
          var newEvent = DuplicateToken(eventTemplate);
      
          //Add Event for each button / plugin.
          newEvent["trigger"]["target"] = $"btn{func.Key}";
          newEvent["actions"][0]["param"]["serverPath"] = $"EATT://{func.Key}";
          newEvent["actions"][0]["param"]["clientPath"] = $"{func.Key}.zip";
          newEvent["id"] = $"btn{func.Key}_onClick";
      
          //Add event to app.
          ((JArray)app["Events"]).Add(newEvent);
      
      }
      
      //Get the json for the app and return it.
      appJson = app.ToString();
      
      Success = true; //Woot!

  }
  catch (Exception ex)
  {
      Success = false; //Meh
      ListErrorJson = AddExceptionToList(ex, ListErrorJson);
  }