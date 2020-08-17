/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM 'AS IS' AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using Autodesk.Forge.BIM360;
using NLog;
using BimProjectSetupCommon.Helpers;
using Autodesk.Forge;
using System.Net;
using Autodesk.Forge.Client;
using System.Text;

namespace BimProjectSetupCommon.Workflow
{
    public class ThreeLeggedWorkflow : BaseWorkflow
    {
        private static bool           m_ThreeLeggedTokenInitialized = false;
        private static ThreeLeggedApi m_threeLeggedApi = new ThreeLeggedApi();
        private static string         s_token = null;
        private static                DateTime s_dt;

        // Declare a local web listener to wait for the oAuth callback on the local machine.
        // Please read this article to configure your local machine properly
        // http://stackoverflow.com/questions/4019466/httplistener-access-denied
        //   ex: netsh http add urlacl url=http://+:3006/oauth user=cyrille
        // Embedded webviews are strongly discouraged for oAuth - https://developers.google.com/identity/protocols/OAuth2InstalledApp
        private static HttpListener m_httpListener = null;

        public delegate void NewBearerDelegate(dynamic bearer);

        private static Scope[] _scope = new Scope[] { Scope.DataRead, Scope.DataWrite };


        public ThreeLeggedWorkflow(AppOptions options ) : base(options)
        {
            m_ThreeLeggedTokenInitialized = false;
        }


        public void initToken()
        {
            try
            {
                Log.Info($"Initialize web listerner to get 3 legged token");
                if (!HttpListener.IsSupported)
                {
                    Log.Warn($"HttpListener is not supported on this platform.");
                    return;
                }

                // Initialize our web listerner
                m_httpListener = new HttpListener();
                m_httpListener.Prefixes.Add(_options.ForgeCallback.Replace("localhost", "+") + "/");
                m_httpListener.Start();
                //IAsyncResult result =_httpListener.BeginGetContext (new AsyncCallback (_3leggedAsyncWaitForCode), _httpListener) ;
                IAsyncResult result = m_httpListener.BeginGetContext(_3leggedAsyncWaitForCode, new NewBearerDelegate(gotit));

                // Generate a URL page that asks for permissions for the specified scopes, and call our default web browser.
                string oauthUrl = m_threeLeggedApi.Authorize(_options.ForgeClientId, oAuthConstants.CODE, _options.ForgeCallback, _scope);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(oauthUrl));

                Log.Info($"Wait to get 3 legged token...");
                Log.Info($"");

                //result.AsyncWaitHandle.WaitOne () ;
                //_httpListener.Stop () ;
            }
            catch (Exception ex)
            {
                Log.Error( ex );
            }
        }


        internal static async void _3leggedAsyncWaitForCode(IAsyncResult ar)
        {
            try
            {
                // Our local web listener was called back from the Autodesk oAuth server
                // That means the user logged properly and granted our application access
                // for the requested scope.
                // Let's grab the code fron the URL and request or final access_token

                //HttpListener listener =(HttpListener)result.AsyncState ;
                var context = m_httpListener.EndGetContext(ar);
                string code = context.Request.QueryString[oAuthConstants.CODE];

                // The code is only to tell the user, he can close is web browser and return
                // to this application.
                var responseString = "<html><body>You can now close this window!</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                var response = context.Response;
                response.ContentType = "text/html";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // Now request the final access_token
                if (!string.IsNullOrEmpty(code))
                {
                    // Call the asynchronous version of the 3-legged client with HTTP information
                    // HTTP information will help you to verify if the call was successful as well
                    // as read the HTTP transaction headers.
                    ApiResponse<dynamic> bearer = await m_threeLeggedApi.GettokenAsyncWithHttpInfo(_options.ForgeClientId, _options.ForgeClientSecret, oAuthConstants.AUTHORIZATION_CODE, code, _options.ForgeCallback);
                    //if ( bearer.StatusCode != 200 )
                    //	throw new Exception ("Request failed! (with HTTP response " + bearer.StatusCode + ")") ;

                    // The JSON response from the oAuth server is the Data variable and has been
                    // already parsed into a DynamicDictionary object.

                    //string token =bearer.Data.token_type + " " + bearer.Data.access_token ;
                    //DateTime dt =DateTime.Now ;
                    //dt.AddSeconds (double.Parse (bearer.Data.expires_in.ToString ())) ;

                    ((NewBearerDelegate)ar.AsyncState)?.Invoke(bearer.Data);
                }
                else
                {
                    Log.Warn($"Failed to get the authorization code");
                    ((NewBearerDelegate)ar.AsyncState)?.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex );
                ((NewBearerDelegate)ar.AsyncState)?.Invoke(null);
            }
            finally
            {
                Log.Info($"Stop web http server.");
                m_httpListener.Stop();
            }
        }


        private static void gotit(dynamic bearer)
        {
            if (bearer == null)
            {
                Log.Error($"Failed to get the access token, Authentication failed!");
                return;
            }
            // The call returned successfully and you got a valid access_token.
            s_token = bearer.access_token;
            s_dt = DateTime.Now;
            s_dt.AddSeconds(double.Parse(bearer.expires_in.ToString()));
            m_ThreeLeggedTokenInitialized = true;

            Log.Info($"You are logged in, 3 legged token is setup successfully.");
        }

        public new string GetToken()
        {
            return s_token;
        }


        public static bool TokenInitialized
        {
            get { return m_ThreeLeggedTokenInitialized; }
        }
    }
}
