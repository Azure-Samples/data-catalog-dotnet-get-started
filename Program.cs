//Microsoft Data Catalog team sample

using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.IO;

namespace ConsoleApplication
{
    class Program
    {
        static string clientIDFromAzureAppRegistration = "{ClientID}";
        static AuthenticationResult authResult = null;

	//Note: This example uses the "DefaultCatalog" keyword to update the user's default catalog.  You may alternately
        //specify the actual catalog name.
        static string catalogName = "DefaultCatalog";

        static void Main(string[] args)
        {
            RegisterDataAsset(catalogName, SampleJson("OrdersSample"));
            Console.WriteLine("Registered data asset. Press Enter to continue");
            Console.ReadLine();

            //Search a name
            string searchTerm = "name:=OrdersSample";

            string searchJson = SearchDataAsset(catalogName, searchTerm);

            //Save to search JSON so that you can examine the JSON
            //  The json is saved in the \bin\debug folder of the sample app path
            //  For example, C:\Projects\Data Catalog\Samples\Get started creating a Data Catalog app\bin\Debug\searchJson.txt
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "searchJson.txt", searchJson);

            Console.WriteLine(searchJson);

            Console.WriteLine();
            Console.WriteLine("Searched data asset. Press Enter to continue");
            Console.ReadLine();

            Console.WriteLine("Register sample data asset to Delete. Press Enter to continue");

            //Register a sample data asset to delete
            string dataAssetID = RegisterDataAsset(catalogName, SampleJson("DeleteSample"));
            Console.ReadLine();

            Console.WriteLine("Delete data asset. Press Enter to continue");
            
            DeleteDataAsset(catalogName, dataAssetID);

            Console.ReadLine();
        }

        //Get access token:
        // To call a Data Catalog REST operation, create an instance of AuthenticationContext and call AcquireToken
        // AuthenticationContext is part of the Active Directory Authentication Library NuGet package
        // To install the Active Directory Authentication Library NuGet package in Visual Studio, 
        //  run "Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory" from the NuGet Package Manager Console.
        static AuthenticationResult AccessToken()
        {
            if (authResult == null)
            {
                //Resource Uri for Data Catalog API
                string resourceUri = "https://datacatalog.azure.com";

                //To learn how to register a client app and get a Client ID, see https://msdn.microsoft.com/en-us/library/azure/mt403303.aspx#clientID   
                string clientId = clientIDFromAzureAppRegistration;

                //A redirect uri gives AAD more details about the specific application that it will authenticate.
                //Since a client app does not have an external service to redirect to, this Uri is the standard placeholder for a client app.
                string redirectUri = "https://login.live.com/oauth20_desktop.srf";

                // Create an instance of AuthenticationContext to acquire an Azure access token
                // OAuth2 authority Uri
                string authorityUri = "https://login.windows.net/common/oauth2/authorize";
                AuthenticationContext authContext = new AuthenticationContext(authorityUri);

                // Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
                //  AcquireToken takes a Client Id that Azure AD creates when you register your client app.
                authResult = authContext.AcquireToken(resourceUri, clientId, new Uri(redirectUri), PromptBehavior.RefreshSession);
            }

            return authResult;
        }

        //Register data asset:
        // The Register Data Asset operation registers a new data asset 
        // or updates an existing one if an asset with the same identity already exists. 
        static string RegisterDataAsset(string catalogName, string json)
        {
            string dataAssetHeader = string.Empty;

            //Get access token to use to call operation
            AuthenticationResult authResult = AccessToken();

            string fullUri = string.Format("https://{0}.datacatalog.azure.com/{1}/views/tables?api-version=2015-07.1.0-Preview",
                authResult.TenantId, catalogName);

            //Create a POST WebRequest as a Json content type
            HttpWebRequest request = System.Net.WebRequest.Create(fullUri) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentLength = 0;
            request.ContentType = "application/json";

            //To authorize the operation call, you need an access token which is part of the Authorization header
            request.Headers.Add("Authorization", authResult.CreateAuthorizationHeader());

            //POST web request
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;

            //Write JSON byte[] into a Stream and get web response
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteArray, 0, byteArray.Length);

                try
                {
                    var response = (HttpWebResponse)request.GetResponse();

                    //Get the Response header which contains the data asset ID
                    //The format is: tables/{data asset ID} 
                    dataAssetHeader = response.Headers["Location"];
                }
                catch(WebException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.Status);
                    if (ex.Response != null)
                    {
                        // can use ex.Response.Status, .StatusDescription
                        if (ex.Response.ContentLength != 0)
                        {
                            using (var stream = ex.Response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    Console.WriteLine(reader.ReadToEnd());
                                }
                            }
                        }
                    }
                    return null;
                }
            }

            return dataAssetHeader;
        }

        //Search data asset:
        //The Search Data Asset operation searches over data assets based on the search terms provided.
        static string SearchDataAsset(string catalogName, string searchTerm)
        {
            string responseContent = string.Empty;

            //Get access token to use to call operation
            AuthenticationResult authResult = AccessToken();

            //NOTE: To find the Catalog Name, sign into Azure Data Catalog, and choose User. You will see a list of Catalog names.          
            string fullUri =
                string.Format("https://{0}.search.datacatalog.azure.com/{1}/search/search?searchTerms={2}&count=10&api-version=2015-06.0.1-Preview",
                authResult.TenantId, catalogName, searchTerm);

            //Create a GET WebRequest
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUri);
            request.Method = "GET";

            //To authorize the operation call, you need an access token which is part of the Authorization header
            request.Headers.Add("Authorization", authResult.CreateAuthorizationHeader());

            try
            {
                //Get HttpWebResponse from GET request
                using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
                {
                    //Get StreamReader that holds the response stream
                    using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                    {
                        responseContent = reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Status);
                if (ex.Response != null)
                {
                    // can use ex.Response.Status, .StatusDescription
                    if (ex.Response.ContentLength != 0)
                    {
                        using (var stream = ex.Response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                Console.WriteLine(reader.ReadToEnd());
                            }
                        }
                    }
                }
                return null;
            }

            return responseContent;
        }

        //Delete data asset:
        // The Delete Data Asset operation deletes a data asset and all annotations (if any) attached to it. 
        static string DeleteDataAsset(string catalogName, string dataAssetID)
        {
            string responseStatusCode = string.Empty;

            //Get access token to use to call operation
            AuthenticationResult authResult = AccessToken();

            //NOTE: To find the Catalog Name, sign into Azure Data Catalog, and choose User. You will see a list of Catalog names.          
            string fullUri =
                string.Format("https://{0}.datacatalog.azure.com/{1}/views/{2}?api-version=2015-07.1.0-Preview",
                authResult.TenantId, catalogName, dataAssetID);

            //Create a DELETE WebRequest as a Json content type
            HttpWebRequest request = System.Net.WebRequest.Create(fullUri) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "DELETE";
            request.ContentLength = 0;
            request.ContentType = "application/json";

            //To authorize the operation call, you need an access token which is part of the Authorization header
            request.Headers.Add("Authorization", authResult.CreateAuthorizationHeader());

            try
            {
                //Get HttpWebResponse from GET request
                using (HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse)
                {
                    responseStatusCode = response.StatusCode.ToString();
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Status);
                if (ex.Response != null)
                {
                    // can use ex.Response.Status, .StatusDescription
                    if (ex.Response.ContentLength != 0)
                    {
                        using (var stream = ex.Response.GetResponseStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                Console.WriteLine(reader.ReadToEnd());
                            }
                        }
                    }
                }
                return null;
            }

            return responseStatusCode;
        }

        static string SampleJson(string name)
        {
            return "{" +
                "\"__creatorId\": \"SQL Server\"," +
                "\"name\": \"" + name + "\", " +
                "\"dataSource\": { " +
                    "\"sourceType\": \"SQL Server\", " +
                    "\"objectType\": \"Table\"," +
                    "\"formatType\": \"Structured\"" +
                "}," +
                "\"dsl\": {" +
                    "\"protocol\": \"tds\"," +
                    "\"authentication\": \"windows\"," +
                    "\"address\": {" +
                        "\"server\": \"MyServer.contoso.com\"," +
                        "\"database\": \"Northwind\"," +
                        "\"schema\": \"dbo\"," +
                        "\"object\": \"" + name + "\", " +
                    "}" +
                "}," +
                "\"modifiedTime\": \"2015-05-15T03:48:39.2425547Z\"," +
                "\"lastRegisteredTime\": \"2015-05-15T03:48:39.2425547Z\"," +
                "\"lastRegisteredBy\": {" +
                    "\"upn\": \"user1@contoso.com\"," +
                    "\"firstName\": \"User1FirstName\"," +
                    "\"lastName\": \"User1LastName\"" +
                "}," +
                "\"schemas\": [" +
                    "{" +
                        "\"__creatorId\": \"SQL Server\"," +
                        "\"modifiedTime\": \"2015-05-15T03:48:39.2425547Z\"," +
                        "\"columns\": [" +
                            "{" +
                                "\"name\": \"OrderID\"," +
                                "\"isNullable\": false," +
                                "\"type\": \"int\"," +
                                "\"maxLength\": 4," +
                                "\"precision\": 10" +
                            "}," +
                            "{" +
                                "\"name\": \"CustomerID\"," +
                                "\"isNullable\": true," +
                                "\"type\": \"nchar\"," +
                                "\"maxLength\": 10," +
                                "\"precision\": 0" +
                            "}," +
                            "{" +
                                "\"name\": \"OrderDate\"," +
                                "\"isNullable\": true," +
                                "\"type\": \"datetime\"," +
                                "\"maxLength\": 8," +
                                "\"precision\": 23" +
                            "}," +
                        "]," +
                    "}" +
                "]" +
            "}";
        }
    }
    
}
