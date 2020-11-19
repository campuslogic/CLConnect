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
                string apiURL;

                // if DisableAutoUpdate, use the API endpoints from web.config
                if (string.Equals(
                    ConfigurationManager.AppSettings["DisableAutoUpdate"],
                    "true",
                    StringComparison.InvariantCultureIgnoreCase
                ))
                {
                    stsUrl = ConfigurationManager.AppSettings["StsUrl"];
                    string pmApiUrl = ConfigurationManager.AppSettings["PmWebApiUrl"];

                    if (pmApiUrl.IsNullOrWhiteSpace())
                    {
                        throw new Exception("App setting 'PmWebApiUrl' not found");
                    }
                    apiURL = pmApiUrl;
                }
                // else, normal check
                else
                {
                    switch (environment)
                    {
                        case EnvironmentConstants.SANDBOX:
                        {
                            apiURL = ApiUrlConstants.PM_API_URL_SANDBOX;
                            stsUrl = ApiUrlConstants.STS_URL_SANDBOX;
                            break;
                        }
                        case EnvironmentConstants.PRODUCTION:
                        {
                            apiURL = ApiUrlConstants.PM_API_URL_PRODUCTION;
                            stsUrl = ApiUrlConstants.STS_URL_PRODUCTION;
                            break;
                        }
                        default:
                        {
                            apiURL = ApiUrlConstants.PM_API_URL_SANDBOX;
                            stsUrl = ApiUrlConstants.STS_URL_SANDBOX;
                            break;
                        }
                    }
                }
                CredentialsManager credentialsManager = new CredentialsManager();
                HttpResponseMessage response = credentialsManager.GetAuthorizationToken(username, password, apiURL, stsUrl);

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