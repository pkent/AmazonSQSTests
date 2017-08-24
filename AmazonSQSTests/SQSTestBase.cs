using Amazon.SQS;
using AmazonSQS.Tests.API.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonSQS.Tests.API
{
    public class SQSTestBase : IDisposable
    {
        protected AmazonSQSClient _sqs;
        protected string _queueUrl;

        /// <summary>
        /// Note: xUnit will create an instance of the test class on every test case hence each test
        /// will have a new client.
        /// </summary>
        public SQSTestBase()
        {
            _sqs = LocalSQSClientBuilder.CreateClient();
        }

        /// <summary>
        /// Cleanup after every test case is run.
        /// </summary>
        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_queueUrl))
            {
                SqsQueueUtils.DeleteQueue(_queueUrl, _sqs);
            }
        }
    }
}
