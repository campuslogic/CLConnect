﻿using log4net;
using System.Web.Http;
using System.Net.Http;
using CampusLogicEvents.Web.Filters;

namespace CampusLogicEvents.Web.WebAPI
{
    [LocalRequestOnly]
    public class IsirCorrectionsController : FolderPickerController
    {
        private static readonly ILog logger = LogManager.GetLogger("AdoNetAppender");

        /// <summary>
        /// Constructor for the AwardLetterUploadController
        /// </summary>
        public IsirCorrectionsController()
        {
        }

        [HttpGet]
        public HttpResponseMessage OpenFolderExplorer(string directoryPath)
        {
            return base.OpenFolderExplorer(directoryPath);
        }

        [HttpGet]
        public HttpResponseMessage TestFolderPermissions(string directoryPath)
        {
            return base.TestWritePermissions(directoryPath);
        }
    }
}