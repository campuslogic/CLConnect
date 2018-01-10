﻿using CampusLogicEvents.Implementation;
using CampusLogicEvents.Implementation.Configurations;
using CampusLogicEvents.Implementation.Models;
using Hangfire;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace CampusLogicEvents.Web.Models
{
    public static class BatchProcessingService
    {
        private static readonly ILog logger = LogManager.GetLogger("AdoNetAppender");
        private static readonly int RETRY_MAX = 3;
        private static readonly NotificationManager notificationManager = new NotificationManager();
        private static readonly CampusLogicSection campusLogicConfigSection = (CampusLogicSection)ConfigurationManager.GetSection(ConfigConstants.CampusLogicConfigurationSectionName);
        
        [AutomaticRetry(Attempts = 0)]
        public static async void RunBatchProcess(string type, string name, int size)
        {

            logger.Info("enter batch processing");
            Guid processGuid = Guid.NewGuid();
            using (var dbContext = new CampusLogicContext())
            {
                try
                {
                    // Get all records in our LocalDB to process
                    var records = dbContext.BatchProcessRecords.Where(b => b.Name == name && b.Type == type && b.ProcessGuid == null);

                    if (records.Any())
                    {
                        // Lock these records from being processed again
                        dbContext.Database.ExecuteSqlCommand($"UPDATE[dbo].[BatchProcessRecord] SET[ProcessGuid] = '{processGuid}' FROM [dbo].[BatchProcessRecord] WHERE[Id] IN(SELECT [Id] from[dbo].[BatchProcessRecord] WHERE [Type] = '{type}' AND [Name] = '{name}' AND [ProcessGuid] IS NULL)");

                        // Ensure there are locked records with this process guid
                        if (dbContext.BatchProcessRecords.Any(b => b.ProcessGuid != null && b.ProcessGuid == processGuid))
                        {
                            if (type == ConfigConstants.AwardLetterPrintBatchType)
                            {
                                var manager = new DocumentManager();
                                Dictionary<int,Guid> recordIds = new Dictionary<int, Guid>();

                                var recordList = dbContext.BatchProcessRecords.Where(b => b.ProcessGuid == processGuid).Select(b => b).ToList();
                                // Get all records with this process guid
                                foreach (var record in recordList)
                                {
                                    // Deseralize the message
                                    EventNotificationData eventData =
                                        JsonConvert.DeserializeObject<EventNotificationData>(record.Message);
                                    
                                    //Track retry attempts
                                    var now = DateTime.Now;
                                    var processSingularly = false;
                                    var retryTimeHasPassed = record.RetryUpdatedDate.HasValue ? record.RetryUpdatedDate.Value.AddHours(1) < DateTime.Now : true;
                                    if (record.RetryCount == null)
                                    {
                                        record.RetryCount = 1;
                                        dbContext.Database.ExecuteSqlCommand($"UPDATE[dbo].[BatchProcessRecord] SET [RetryCount] = 1, [RetryUpdatedDate] = '{now}' FROM [dbo].[BatchProcessRecord] WHERE[Id] = {record.Id}");
                                    }
                                    else if (retryTimeHasPassed || record.RetryCount == RETRY_MAX)
                                    {
                                        record.RetryCount = record.RetryCount + 1;
                                        dbContext.Database.ExecuteSqlCommand($"UPDATE[dbo].[BatchProcessRecord] SET [RetryCount] = [RetryCount] + 1, [RetryUpdatedDate] = '{now}'  FROM [dbo].[BatchProcessRecord] WHERE[Id] = {record.Id}");
                                    }

                                    if (record.RetryCount > RETRY_MAX)
                                    {
                                        SendErrorNotification("Batch AwardLetter process", $"This record has reached it's maximum retry attempts, record Id:  {eventData.AlRecordId.Value}. Please contact your CampusLogic contact for next steps.");
                                        logger.Error($"Record for batch awardletter process has reached maximum retry attempts, with award letter record Id: {eventData.AlRecordId.Value}.");
                                        dbContext.Database.ExecuteSqlCommand($"DELETE FROM [dbo].[BatchProcessRecord] WHERE [ProcessGuid] = '{processGuid}' and [Id] = {record.Id}");
                                    }
                                    else if (record.RetryCount == RETRY_MAX && retryTimeHasPassed)
                                    {
                                        processSingularly = true;
                                        recordIds.Add(record.Id, eventData.AlRecordId.Value);
                                    }
                                    else if (retryTimeHasPassed)
                                    {
                                        processSingularly = false;
                                        recordIds.Add(record.Id, eventData.AlRecordId.Value);
                                    }

                                    if (recordIds.Count == size || processSingularly)
                                    {
                                        var response = await manager.GetBatchAwardLetterPdfFile(recordIds.Values.ToList(), name);
                                        //If the response was successful remove those records from the db so
                                        //we do not continue to process them if the file fails
                                        if (response.IsSuccessStatusCode)
                                        {
                                            var recordIdList = string.Join(",", recordIds.Keys);
                                            dbContext.Database.ExecuteSqlCommand($"DELETE FROM [dbo].[BatchProcessRecord] WHERE [ProcessGuid] = '{processGuid}' and [Id] IN ({recordIdList})");
                                        }
                                        recordIds.Clear();
                                    }
                                }

                                // Process the last group of records that was smaller than the batch size
                                if (recordIds.Any())
                                {
                                    var response = await manager.GetBatchAwardLetterPdfFile(recordIds.Values.ToList(), name);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var recordIdList = string.Join(",", recordIds.Keys);
                                        dbContext.Database.ExecuteSqlCommand($"DELETE FROM [dbo].[BatchProcessRecord] WHERE [ProcessGuid] = '{processGuid}' and [Id] IN ({recordIdList})");
                                    }
                                }
                                dbContext.Database.ExecuteSqlCommand($"UPDATE [dbo].[BatchProcessRecord] SET [ProcessGuid] = NULL WHERE [ProcessGuid] = '{processGuid}'");
                            }
                        }
                    }
                }

                catch (TaskCanceledException ex)
                {
                    logger.Error($"The task was canceled: {ex}");
                    if (ex.InnerException != null)
                    {
                        logger.Error($"Inner exception: {ex.InnerException}");
                    }
                }
                catch (Exception e)
                {
                    //Something happened during processing. Update any records that may have been marked for processing back to null so that they can be re-processed.
                    logger.Error($"An error occured while attempting to execute the batch process: {e}");
                    dbContext.Database.ExecuteSqlCommand($"UPDATE [dbo].[BatchProcessRecord] SET [ProcessGuid] = NULL WHERE [ProcessGuid] = '{processGuid}'");
                }
            }
        }

        /// <summary>
		/// Send error notification and return
		/// information to be logged back in web
		/// layer
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="errorMessage"></param>
		/// <returns></returns>
		private static NotificationData SendErrorNotification(string operation, string errorMessage, Exception ex = null)
        {
            if (campusLogicConfigSection.SMTPSettings.NotificationsEnabled ?? false)
            {
                return ex == null ? notificationManager.SendErrorNotification(operation, errorMessage).Result : notificationManager.SendErrorNotification(operation, ex).Result;
            }
            else
            {
                return new NotificationData();
            }
        }
    }
}