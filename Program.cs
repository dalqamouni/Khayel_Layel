using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Model;
using Newtonsoft.Json;

namespace BulkRecordingDownloader
{
    internal class Program
    {
        private static ConversationsApi conversationsApi;
        private static RecordingApi recordingApi;
        private static ApiClient apiClient;
        private static String clientId;
        private static String clientSecret;
        private static String dates = "2025-02-25T00:00:00.000Z/2025-02-25T23:59:00.000Z";
        private static string FolderName = "2025-02-25";
        private static BatchDownloadJobSubmission batchRequestBody = new BatchDownloadJobSubmission();
        private static List<BatchDownloadRequest> batchDownloadRequestList = new List<BatchDownloadRequest>();

        static void Main(string[] args)
        {
            authentication();

            downloadAllRecordings(dates);
            DownloadOutboundRecordings(dates);

            // Final Output
            Console.WriteLine("DONE");

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }
        }

        public static void authentication()
        {
            //OAuth
            clientId = "485e10e6-44be-4a4f-adf5-f4661cdaa335";
            clientSecret = "dqakz-pDeRaekvSEVfFtz8dgbeLqHNdasIhEFOJpmG4";
            // orgRegion values example: us_east_1
            string orgRegion = "eu-west-1";

            // Set Region
            PureCloudRegionHosts region = PureCloudRegionHosts.eu_central_1;
            Configuration.Default.ApiClient.setBasePath(region);


            // Configure SDK Settings
            var accessTokenInfo = Configuration.Default.ApiClient.PostToken(clientId, clientSecret);
            Configuration.Default.AccessToken = accessTokenInfo.AccessToken;




            // Create API instances
            conversationsApi = new ConversationsApi();
            recordingApi = new RecordingApi();


            Console.WriteLine("Working...");
        }

        public static void downloadAllRecordings(string dates)
        {
            Console.WriteLine("Start batch request process for Inbound Calls.");
            BatchDownloadJobStatusResult completedBatchStatus = new BatchDownloadJobStatusResult();

            // Process and build the request for downloading the recordings
            // Get the conversations within the date interval and start adding them to batch request
            List<ConversationDetailQueryFilter> conversationDetailQueryFilters = new List<ConversationDetailQueryFilter>();
            ConversationDetailQueryFilter ConversationDetailQueryFilter = new ConversationDetailQueryFilter();

            ConversationDetailQueryPredicate divisionPredicate = new ConversationDetailQueryPredicate();
            divisionPredicate.Dimension = ConversationDetailQueryPredicate.DimensionEnum.Divisionid;
            divisionPredicate.Value = "89c0e97b-f8c1-477a-ae0c-97f4ca394702";
            divisionPredicate.Type = ConversationDetailQueryPredicate.TypeEnum.Dimension;
            divisionPredicate.Operator = ConversationDetailQueryPredicate.OperatorEnum.Matches;

            ConversationDetailQueryPredicate inboundCallsPredicate = new ConversationDetailQueryPredicate();
            inboundCallsPredicate.Metric = ConversationDetailQueryPredicate.MetricEnum.Tanswered;
            inboundCallsPredicate.Type = ConversationDetailQueryPredicate.TypeEnum.Metric;
            inboundCallsPredicate.Operator = ConversationDetailQueryPredicate.OperatorEnum.Exists;

            ConversationDetailQueryFilter.Predicates = new List<ConversationDetailQueryPredicate> { divisionPredicate, inboundCallsPredicate };
            ConversationDetailQueryFilter.Type = ConversationDetailQueryFilter.TypeEnum.And;
            conversationDetailQueryFilters.Add(ConversationDetailQueryFilter);

            PagingSpec pagingSpec = new PagingSpec();
            pagingSpec.PageSize = 100;
            pagingSpec.PageNumber = 1;

            AnalyticsConversationQueryResponse conversationDetails = conversationsApi.PostAnalyticsConversationsDetailsQuery(new ConversationQuery(
                conversationDetailQueryFilters,
                null,
                null, null, null, null, null, dates, null, pagingSpec));

            foreach (var conversations in conversationDetails.Conversations)
            {
                addConversationRecordingsToBatch(conversations.ConversationId);
            }

            // Send a batch request and start polling for updates
            BatchDownloadJobSubmissionResult result = recordingApi.PostRecordingBatchrequests(batchRequestBody);
            completedBatchStatus = getRecordingStatus(result);

            // Start downloading the recording files individually
            foreach (var recording in completedBatchStatus.Results)
            {
                downloadRecording(recording, "INBOUND");
            }

            batchDownloadRequestList.Clear();

            int? totalRecords = conversationDetails.TotalHits;
            if (totalRecords != null)
            {
                double? totalPages = (double)totalRecords / pagingSpec.PageSize;
                if (totalPages != null)
                {
                    bool isInt = totalPages % 1 == 0;
                    int totalPagesCount = 0;
                    if (!isInt)
                        totalPagesCount = (int)totalPages + 1;
                    else
                        totalPagesCount = (int)totalPages;

                    for (int i = 1; i < totalPagesCount; i++)
                    {
                        pagingSpec.PageNumber++;
                        conversationDetails = conversationsApi.PostAnalyticsConversationsDetailsQuery(new ConversationQuery(
                            conversationDetailQueryFilters,
                            null,
                            null, null, null, null, null, dates, null, pagingSpec));

                        foreach (var conversations in conversationDetails.Conversations)
                        {
                            addConversationRecordingsToBatch(conversations.ConversationId);
                        }

                        // Send a batch request and start polling for updates
                        result = recordingApi.PostRecordingBatchrequests(batchRequestBody);
                        completedBatchStatus = getRecordingStatus(result);

                        // Start downloading the recording files individually
                        foreach (var recording in completedBatchStatus.Results)
                        {
                            downloadRecording(recording, "INBOUND");
                        }
                        batchDownloadRequestList.Clear();
                    }
                }
            }

        }

        private static void DownloadOutboundRecordings(string dates)
        {
            Console.WriteLine("Start batch request process for Outboubd Calls.");
            BatchDownloadJobStatusResult completedBatchStatus = new BatchDownloadJobStatusResult();

            // Process and build the request for downloading the recordings
            // Get the conversations within the date interval and start adding them to batch request
            List<ConversationDetailQueryFilter> conversationDetailQueryFilters = new List<ConversationDetailQueryFilter>();
            ConversationDetailQueryFilter ConversationDetailQueryFilter = new ConversationDetailQueryFilter();

            ConversationDetailQueryPredicate divisionPredicate = new ConversationDetailQueryPredicate();
            divisionPredicate.Dimension = ConversationDetailQueryPredicate.DimensionEnum.Divisionid;
            divisionPredicate.Value = "d36c139b-b630-4254-aa71-c518726b2564";
            divisionPredicate.Type = ConversationDetailQueryPredicate.TypeEnum.Dimension;
            divisionPredicate.Operator = ConversationDetailQueryPredicate.OperatorEnum.Matches;

            ConversationDetailQueryPredicate outboundCallsPredicate = new ConversationDetailQueryPredicate();
            outboundCallsPredicate.Metric = ConversationDetailQueryPredicate.MetricEnum.Tcontacting;
            outboundCallsPredicate.Type = ConversationDetailQueryPredicate.TypeEnum.Metric;
            outboundCallsPredicate.Operator = ConversationDetailQueryPredicate.OperatorEnum.Exists;

            ConversationDetailQueryFilter.Predicates = new List<ConversationDetailQueryPredicate> { divisionPredicate, outboundCallsPredicate };
            ConversationDetailQueryFilter.Type = ConversationDetailQueryFilter.TypeEnum.And;
            conversationDetailQueryFilters.Add(ConversationDetailQueryFilter);

            PagingSpec pagingSpec = new PagingSpec();
            pagingSpec.PageSize = 100;
            pagingSpec.PageNumber = 1;

            AnalyticsConversationQueryResponse conversationDetails = conversationsApi.PostAnalyticsConversationsDetailsQuery(new ConversationQuery(
                conversationDetailQueryFilters,
                null,
                null, null, null, null, null, dates, null, pagingSpec));

            foreach (var conversations in conversationDetails.Conversations)
            {
                addConversationRecordingsToBatch(conversations.ConversationId);
            }

            // Send a batch request and start polling for updates
            BatchDownloadJobSubmissionResult result = recordingApi.PostRecordingBatchrequests(batchRequestBody);
            completedBatchStatus = getRecordingStatus(result);

            // Start downloading the recording files individually
            foreach (var recording in completedBatchStatus.Results)
            {
                downloadRecording(recording, "OUTBOUND");
            }
            batchDownloadRequestList.Clear();

            int? totalRecords = conversationDetails.TotalHits;
            if (totalRecords != null)
            {
                double? totalPages = (double)totalRecords / pagingSpec.PageSize;
                if (totalPages != null)
                {
                    bool isInt = totalPages % 1 == 0;
                    int totalPagesCount = 0;
                    if (!isInt)
                        totalPagesCount = (int)totalPages + 1;
                    else
                        totalPagesCount = (int)totalPages;

                    for (int i = 1; i < totalPagesCount; i++)
                    {
                        pagingSpec.PageNumber++;
                        conversationDetails = conversationsApi.PostAnalyticsConversationsDetailsQuery(new ConversationQuery(
                            conversationDetailQueryFilters,
                            null,
                            null, null, null, null, null, dates, null, pagingSpec));

                        foreach (var conversations in conversationDetails.Conversations)
                        {
                            addConversationRecordingsToBatch(conversations.ConversationId);
                        }

                        // Send a batch request and start polling for updates
                        result = recordingApi.PostRecordingBatchrequests(batchRequestBody);
                        completedBatchStatus = getRecordingStatus(result);

                        // Start downloading the recording files individually
                        foreach (var recording in completedBatchStatus.Results)
                        {
                            downloadRecording(recording, "OUTBOUND");
                        }
                        batchDownloadRequestList.Clear();
                    }
                }
            }



        }

        public static void addConversationRecordingsToBatch(string conversationId)
        {
            try
            {
                List<RecordingMetadata> recordingsData = recordingApi.GetConversationRecordingmetadata(conversationId);

                // Iterate through every result, check if there are one or more recordingIds in every conversation
                foreach (var recording in recordingsData)
                {
                    BatchDownloadRequest batchRequest = new BatchDownloadRequest();
                    batchRequest.ConversationId = recording.ConversationId;
                    batchRequest.RecordingId = recording.Id;

                    batchDownloadRequestList.Add(batchRequest);
                    batchRequestBody.BatchDownloadRequestList = batchDownloadRequestList;

                    Console.WriteLine("Added " + recording.ConversationId + " to batch request");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured in Adding Recording to Batch having Conversation ID ==> "+ conversationId);
                Console.WriteLine(ex.ToString());
            }

        }

        // Plot conversationId and recordingId to request for batchdownload Recordings
        private static BatchDownloadJobStatusResult getRecordingStatus(BatchDownloadJobSubmissionResult recordingBatchRequest)
        {
            Console.WriteLine("Processing the recordings...");
            BatchDownloadJobStatusResult result = new BatchDownloadJobStatusResult();

            result = recordingApi.GetRecordingBatchrequest(recordingBatchRequest.Id);

            if (result.ExpectedResultCount != result.ResultCount)
            {
                Console.WriteLine("Batch Result Status:" + result.ResultCount + " / " + result.ExpectedResultCount);

                // Simple polling through recursion
                Thread.Sleep(5000);
                return getRecordingStatus(recordingBatchRequest);
            }

            // Once result count reach expected.
            return result;
        }

        // Download Recordings
        private static void downloadRecording(BatchDownloadJobResult recording, string callType)
        {
            Console.WriteLine("Downloading now. Please wait...");

            String conversationId = recording.ConversationId;
            String recordingId = recording.RecordingId;
            String sourceURL = recording.ResultUrl;
            String errorMsg = recording.ErrorMsg;

            String targetDirectory = "C:\Users\mohammad.dalqamouni\Desktop\recording in bulk";

            // If there is an errorMsg skip the recording download
            if (errorMsg != null)
            {
                Console.WriteLine("Skipping this recording. Reason: " + errorMsg);
                return;
            }

            // Download the recording if available
            String ext = getExtension(recording);

            string filename = conversationId + "_" + recordingId;
            string path = targetDirectory + "\\" + FolderName + "\\" + callType;
            CreateIfMissing(path);

            using (WebClient wc = new WebClient())
                wc.DownloadFile(sourceURL, path +"\\" + filename + "." + ext);
        }


        // Get extension of a recording
        private static string getExtension(BatchDownloadJobResult recording)
        {
            // Store the contentType to a variable that will be used later to determine the extension of recordings.
            string contentType = recording.ContentType;

            // Split the text and gets the extension that will be used for the recording
            string ext = contentType.Split('/').Last();

            // For the JSON special case
            if (ext.Length >= 4)
            {
                ext = ext.Substring(0, 4);
            }

            return ext;
        }

        private static void CreateIfMissing(string path)
        {
            bool folderExists = Directory.Exists(path);
            if (!folderExists)
                Directory.CreateDirectory(path);
        }
    }
}
