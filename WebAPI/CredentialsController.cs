using log4net;
using System.Net.Http;
using System.Web.Http;
using CampusLogicEvents.Implementation;
using System;
using System.Net;
using System.Collections.Generic;
using System.Configuration;
using CampusLogicEvents.Implementation.Configurations;
using Microsoft.Ajax.Utilities;

namespace CampusLogicEvents.Web.WebAPI
{
    public class CredentialsController : ApiController
    {

        private static readonly ILog logger = LogManager.GetLogger("AdoNetAppender");

        public CredentialsController()
        {
        }

        [HttpGet]
        public HttpResponseMessage TestAPICredentials(string username, string password, string environment, bool awardLetterUploadEnabled = false)
        {
            try
            {
                string stsUrl;
                List<string> apiURLs = new List<string>();

                // if DisableAutoUpdate, use the API endpoints from web.config
                if (string.Equals(
                    ConfigurationManager.AppSettings["DisableAutoUpdate"],
                    "true",
                    StringComparison.InvariantCultureIgnoreCase
                ))
                {
                    stsUrl = ConfigurationManager.AppSettings["StsUrl"];

                    string svApiUrl = ConfigurationManager.AppSettings["SvWebApiUrl"];
                    string pmApiUrl = ConfigurationManager.AppSettings["PmWebApiUrl"];
                    string alApiUrl = ConfigurationManager.AppSettings["AwardLetterWebAPIURL"];
                    string suApiUrl = ConfigurationManager.AppSettings["SuWebApiUrl"];
                    string saApiUrl = ConfigurationManager.AppSettings["SaWebApiUrl"];

                    if (!svApiUrl.IsNullOrWhiteSpace())
                    {
                        apiURLs.Add(svApiUrl);
                    }

                    if (!pmApiUrl.IsNullOrWhiteSpace())
                    {
                        apiURLs.Add(pmApiUrl);
                    }

                    if (!alApiUrl.IsNullOrWhiteSpace())
                    {
                        apiURLs.Add(alApiUrl);
                    }

                    if (!suApiUrl.IsNullOrWhiteSpace())
                    {
                        apiURLs.Add(suApiUrl);
                    }

                    if (!saApiUrl.IsNullOrWhiteSpace())
                    {
                        apiURLs.Add(saApiUrl);
                    }
                }
                // else, normal check
                else
                {
                    switch (environment)
                    {
                        case EnvironmentConstants.SANDBOX:
                        {
                            apiURLs.Add(ApiUrlConstants.SV_API_URL_SANDBOX);
                            apiURLs.Add(ApiUrlConstants.PM_API_URL_SANDBOX);
                            if (awardLetterUploadEnabled)
                            {
                                apiURLs.Add(ApiUrlConstants.AL_API_URL_SANDBOX);
                            }
                            stsUrl = ApiUrlConstants.STS_URL_SANDBOX;
                            break;
                        }
                        case EnvironmentConstants.PRODUCTION:
                        {
                            apiURLs.Add(ApiUrlConstants.SV_API_URL_PRODUCTION);
                            apiURLs.Add(ApiUrlConstants.PM_API_URL_PRODUCTION);
                            if (awardLetterUploadEnabled)
                            {
                                apiURLs.Add(ApiUrlConstants.AL_API_URL_PRODUCTION);
                            }
                            stsUrl = ApiUrlConstants.STS_URL_PRODUCTION;
                            break;
                        }
                        default:
                        {
                            apiURLs.Add(ApiUrlConstants.SV_API_URL_SANDBOX);
                            apiURLs.Add(ApiUrlConstants.PM_API_URL_SANDBOX);
                            if (awardLetterUploadEnabled)
                            {
                                apiURLs.Add(ApiUrlConstants.AL_API_URL_SANDBOX);
                            }
                            stsUrl = ApiUrlConstants.STS_URL_SANDBOX;
                            break;
                        }
                    }
                }
                CredentialsManager credentialsManager = new CredentialsManager();
                HttpResponseMessage response = credentialsManager.GetAuthorizationToken(username, password, apiURLs, stsUrl);

                return response;
            }
            catch (Exception ex)
            {
                logger.ErrorFormat("TestAPICredentials Get Error: {0}", ex);
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
            }
        }
    }
}