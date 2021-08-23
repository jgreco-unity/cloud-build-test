using System.Collections.Generic;

namespace TestPlatforms.Cloud
{
    public class MockCloudTestClient : ICloudTestClient
    {
        internal TestResultsResponse mockTestResults = new TestResultsResponse();

        private UploadUrlResponse mockUploadUrlResponse = new UploadUrlResponse{ id = "fake-id", upload_uri = "http://fake/uri"};
        private JobStatusResponse mockJobStatusResponse = new JobStatusResponse{ jobId = "fake-id", status = "COMPLETED"};

        public UploadUrlResponse GetUploadURL()
        {
            return mockUploadUrlResponse;
        }

        public UploadUrlResponse GetUploadURL(string accessToken, string projectId)
        {
            return mockUploadUrlResponse;
        }

        public void UploadBuildToUrl(string uploadURL, string buildPath) {}

        public UploadUrlResponse UploadBuild(string buildPath, string accessToken, string projectId)
        {
            return mockUploadUrlResponse;
        }

        public JobStatusResponse RunCloudTests(string buildId, List<string> cloudTests)
        {
            return mockJobStatusResponse;
        }

        public JobStatusResponse RunCloudTests(string buildId, List<string> cloudTests, string accessToken, string projectId)
        {
            return mockJobStatusResponse;
        }

        public JobStatusResponse GetJobStatus(string jobId)
        {
            return mockJobStatusResponse;
        }

        public JobStatusResponse GetJobStatus(string jobId, string accessToken, string projectId)
        {
            return mockJobStatusResponse;
        }

        public TestResultsResponse GetTestResults(string jobId)
        {
            return mockTestResults;
        }

        public TestResultsResponse GetTestResults(string jobId, string accessToken, string projectId)
        {
            return mockTestResults;
        }
    }
}