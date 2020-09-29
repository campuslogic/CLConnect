using log4net;
using System.Net.Http;
using System.Web.Http;
using CampusLogicEvents.Implementation;
using System;
using System.Net;
using System.Collections.Generic;
using CampusLogicEvents.Implementation.Configurations;

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
                string stsURL;
                List<string> apiURLs = new List<string>();

                switch (environment)
                {
                    case EnvironmentConstants.QA:
                    {
                        apiURLs.Add(ApiUrlConstants.SV_API_URL_QA);
                        apiURLs.Add(ApiUrlConstants.PM_API_URL_QA);
                        if (awardLetterUploadEnabled)
                        {
                            apiURLs.Add(ApiUrlConstants.AL_API_URL_QA);
                        }
                        stsURL = ApiUrlConstants.STS_URL_QA;
                        break;
                    }
                    case EnvironmentConstants.SANDBOX:
                    {
                        apiURLs.Add(ApiUrlConstants.SV_API_URL_SANDBOX);
                        apiURLs.Add(ApiUrlConstants.PM_API_URL_SANDBOX);
                        if (awardLetterUploadEnabled)
                        {
                            apiURLs.Add(ApiUrlConstants.AL_API_URL_SANDBOX);
                        }
                        stsURL = ApiUrlConstants.STS_URL_SANDBOX;
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
                        stsURL = ApiUrlConstants.STS_URL_PRODUCTION;
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid Environment {environment}");
                    }
                }
                CredentialsManager credentialsManager = new CredentialsManager();
                HttpResponseMessage response = credentialsManager.GetAuthorizationToken(username, password, apiURLs, stsURL);

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