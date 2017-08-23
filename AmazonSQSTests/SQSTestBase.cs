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

        public SQSTestBase()
        {
            _sqs = LocalSQSClientBuilder.CreateClient();
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_queueUrl))
            {
                SqsQueueUtils.DeleteQueue(_queueUrl, _sqs);
            }
        }
    }
}
