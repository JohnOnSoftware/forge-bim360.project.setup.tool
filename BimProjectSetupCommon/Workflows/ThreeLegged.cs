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
using System.Threading;

namespace BimProjectSetupCommon.Workflow
{
    public class ThreeLeggedWorkflow : BaseWorkflow
    {
        private  bool           _threeLeggedTokenInitialized = false;
        private  ThreeLeggedApi _threeLeggedApi = new ThreeLeggedApi();
        private  string         _threeLeggedToken = null;
        private  DateTime      _dt;

        // Declare a local web listener to wait for the oAuth callback on the local machine.
        // Please read this article to configure your local machine properly
        // http://stackoverflow.com/questions/4019466/httplistener-access-denied
        //   ex: netsh http add urlacl url=http://+:3006/oauth user=cyrille
        // Embedded webviews are strongly discouraged for oAuth - https://developers.google.com/identity/protocols/OAuth2InstalledApp
        private static HttpListener _httpListener = null;

        private static readonly Scope[] _scope = new Scope[] { Scope.DataRead, Scope.DataWrite };


        public ThreeLeggedWorkflow(AppOptions options ) : base(options)
        {
            _threeLeggedTokenInitialized = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void initToken()
        {
            _threeLeggedTokenInitialized = false;
            try
            {
                Log.Info($"Initialize web listerner to get 3 legged token");
                if (!HttpListener.IsSupported)
                {
                    Log.Warn($"HttpListener is not supported on this platform.");
                    return;
                }

                // Initialize our web listerner
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(_options.ForgeCallback.Replace("localhost", "+") + "/");
                _httpListener.Start();
                //IAsyncResult result =_httpListener.BeginGetContext (new AsyncCallback (_3leggedAsyncWaitForCode), _httpListener) ;
                IAsyncResult result = _httpListener.BeginGetContext(_3leggedAsyncWaitForCode, null );

                // Generate a URL page that asks for permissions for the specified scopes, and call our default web browser.
                string oauthUrl = _threeLeggedApi.Authorize(_options.ForgeClientId, oAuthConstants.CODE, _options.ForgeCallback, _scope);
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


        internal async void _3leggedAsyncWaitForCode(IAsyncResult ar)
        {
            try
            {
                // Our local web listener was called back from the Autodesk oAuth server
                // That means the user logged properly and granted our application access
                // for the requested scope.
                // Let's grab the code fron the URL and request or final access_token

                //HttpListener listener =(HttpListener)result.AsyncState ;
                var context = _httpListener.EndGetContext(ar);
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
                    ApiResponse<dynamic> bearer = await _threeLeggedApi.GettokenAsyncWithHttpInfo(_options.ForgeClientId, _options.ForgeClientSecret, oAuthConstants.AUTHORIZATION_CODE, code, _options.ForgeCallback);
                    if (bearer.StatusCode != 200 || bearer.Data == null)
                    {
                        Log.Error($"Failed to get the access token, Authentication failed!");
                        return;
                    }

                    // The call returned successfully and you got a valid access_token.
                    _threeLeggedToken = bearer.Data.access_token;
                    _dt = DateTime.Now;

                    Log.Info($"You are logged in, 3 legged token is setup successfully.");


                }
                else
                {
                    Log.Warn($"Failed to get the authorization code");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex );
            }
            finally
            {
                Log.Info($"Stop web http server.");
                _httpListener.Stop();
                _threeLeggedTokenInitialized = true;
            }
        }

        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <returns></returns>
        public new string GetToken()
        {
            if (_threeLeggedToken == null || ((DateTime.Now - _dt) > TimeSpan.FromMinutes(30)))
            {
                initToken();
                while( !TokenInitialized)
                {
                    Thread.Sleep(2000);
                }
                _dt = DateTime.Now;
                return _threeLeggedToken;
            }
            else return _threeLeggedToken;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TokenInitialized
        {
            get { return _threeLeggedTokenInitialized; }
        }
    }
}
