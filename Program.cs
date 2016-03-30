//Microsoft Data Catalog team sample

using System;
using System.Text;
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
            string dataAssetUrl = RegisterDataAsset(catalogName, SampleJson("DeleteSample"));
            Console.ReadLine();

            Console.WriteLine("Delete data asset. Press Enter to continue");

            DeleteDataAsset(dataAssetUrl);

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
                string resourceUri = "https://api.azuredatacatalog.com";

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

            string fullUri = string.Format("https://api.azuredatacatalog.com/catalogs/{0}/views/tables?api-version=2016-03-30", catalogName);

            //Create a POST WebRequest as a Json content type
            HttpWebRequest request = System.Net.WebRequest.Create(fullUri) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "POST";

            try
            {
                var response = SetRequestAndGetResponse(request, json);

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
            return dataAssetHeader;
        }

        //Search data asset:
        //The Search Data Asset operation searches over data assets based on the search terms provided.
        static string SearchDataAsset(string catalogName, string searchTerm)
        {
            string responseContent = string.Empty;

            //NOTE: To find the Catalog Name, sign into Azure Data Catalog, and choose User. You will see a list of Catalog names.          
            string fullUri =
                string.Format("https://api.azuredatacatalog.com/catalogs/{0}/search/search?searchTerms={1}&count=10&api-version=2016-03-30", catalogName, searchTerm);

            //Create a GET WebRequest
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUri);
            request.Method = "GET";

            try
            {
                //Get HttpWebResponse from GET request
                using (HttpWebResponse httpResponse = SetRequestAndGetResponse(request))
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
        static string DeleteDataAsset(string dataAssetUrl)
        {
            string responseStatusCode = string.Empty;

            //NOTE: To find the Catalog Name, sign into Azure Data Catalog, and choose User. You will see a list of Catalog names.          
            string fullUri = string.Format("{0}?api-version=2016-03-30", dataAssetUrl);

            //Create a DELETE WebRequest as a Json content type
            HttpWebRequest request = System.Net.WebRequest.Create(fullUri) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "DELETE";


            try
            {
                //Get HttpWebResponse from GET request
                using (HttpWebResponse response = SetRequestAndGetResponse(request))
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

        static HttpWebResponse SetRequestAndGetResponse(HttpWebRequest request, string payload = null)
        {
            while (true)
            {
                //To authorize the operation call, you need an access token which is part of the Authorization header
                request.Headers.Add("Authorization", AccessToken().CreateAuthorizationHeader());
                //Set to false to be able to intercept redirects
                request.AllowAutoRedirect = false;

                if (!string.IsNullOrEmpty(payload))
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(payload);
                    request.ContentLength = byteArray.Length;
                    request.ContentType = "application/json";
                    //Write JSON byte[] into a Stream
                    request.GetRequestStream().Write(byteArray, 0, byteArray.Length);
                }
                else
                {
                    request.ContentLength = 0;
                }

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                // Requests to **Azure Data Catalog (ADC)** may return an HTTP 302 response to indicate
                // redirection to a different endpoint. In response to a 302, the caller must re-issue
                // the request to the URL specified by the Location response header. 
                if (response.StatusCode == HttpStatusCode.Redirect)
                {
                    string redirectedUrl = response.Headers["Location"];
                    HttpWebRequest nextRequest = WebRequest.Create(redirectedUrl) as HttpWebRequest;
                    nextRequest.Method = request.Method;
                    request = nextRequest;
                }
                else
                {
                    return response;
                }
            }
        }

        static string SampleJson(string name)
        {
            return string.Format(@"
{{
    ""properties"" : {{
        ""fromSourceSystem"" : false,
        ""name"": ""{0}"",
        ""dataSource"": {{
            ""sourceType"": ""SQL Server"",
            ""objectType"": ""Table"",
        }},
        ""dsl"": {{
            ""protocol"": ""tds"",
            ""authentication"": ""windows"",
            ""address"": {{
                ""server"": ""MyServer.contoso.com"",
                ""database"": ""Northwind"",
                ""schema"": ""dbo"",
                ""object"": ""{0}""
            }}
        }},
        ""lastRegisteredBy"": {{
            ""upn"": ""user1@contoso.com"",
            ""firstName"": ""User1FirstName"",
            ""lastName"": ""User1LastName""
        }},
    }},
    ""annotations"" : {{
        ""schema"": {{
            ""properties"" : {{
                ""fromSourceSystem"" : false,
                ""columns"": [
                    {{
                        ""name"": ""OrderID"",
                        ""isNullable"": false,
                        ""type"": ""int"",
                        ""maxLength"": 4,
                        ""precision"": 10
                    }},
                    {{
                        ""name"": ""CustomerID"",
                        ""isNullable"": true,
                        ""type"": ""nchar"",
                        ""maxLength"": 10,
                        ""precision"": 0
                    }},
                    {{
                        ""name"": ""OrderDate"",
                        ""isNullable"": true,
                        ""type"": ""datetime"",
                        ""maxLength"": 8,
                        ""precision"": 23
                    }},
                ],
            }}
        }}
    }}
}}
", name);
        }
    }
}
