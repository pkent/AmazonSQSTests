using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using Amazon;
using Amazon.Runtime;
using AmazonSQS.Tests.API.Utilities;


namespace AmazonSQS.Tests.API
{
    /// <summary>
    /// Tests related to creation of SQS Standard Queues.
    /// </summary>
    public class SQSCreateQueueTests : SQSTestBase
    {
        [Fact]
        public void CreateStandardQueue_WithValidQueueName_ShouldCreateQueue()
        {
            // Setup the queue
            var sqsRequest = new CreateQueueRequest();
            const string queueName = "PositiveTestCase_StandardQueue";
            sqsRequest.QueueName = queueName;
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);

            Assert.True(createQueueResponse.HttpStatusCode == System.Net.HttpStatusCode.OK);
            Assert.EndsWith(queueName, createQueueResponse.QueueUrl);

            _queueUrl = createQueueResponse.QueueUrl;

            // Find the the queue in the list of queues
            ListQueuesRequest listQueuesRequest = new ListQueuesRequest();
            ListQueuesResponse listQueuesResponse = _sqs.ListQueues(listQueuesRequest);

            // Double check the queue is registered in the list of queues
            var createdQueue = listQueuesResponse.QueueUrls.Where(q => q.EndsWith(queueName));
            Assert.True(createdQueue.Count() == 1);
        }

        [Fact]
        public void CreateQueue_WithNoName_ShouldThrowAmazonSQSException()
        {
            var sqsRequest = new CreateQueueRequest();
            var exception = Assert.Throws<AmazonSQSException>(() => _sqs.CreateQueue(sqsRequest));
            Assert.Contains("Invalid request: MissingQueryParamRejection(QueueName)", exception.ErrorCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.StatusCode);
        }


        /// <summary>
        /// The purpose is to try various invalid chars that could be in a queue name and ensure an 
        /// AmazonSQSException occurs. This method is invoked by xUnit once for every item generated in
        /// GetInvalidQueueNames.
        /// 
        /// Valid values should only be alphanumeric characters, hyphens (-), and underscores (_). 
        /// All other characters are considered invalid.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetInvalidQueueNames))]
        public void CreateQueue_WithNameContainingVariousInvalidChars_ShouldThrowAmazonSQSException(string queueName)
        {
            var sqsRequest = new CreateQueueRequest(queueName);

            var exception = Assert.Throws<AmazonSQSException>(() => _sqs.CreateQueue(sqsRequest));
            Assert.Contains("InvalidParameterValue", exception.ErrorCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.StatusCode);
        }

        private static IEnumerable<object[]> GetInvalidQueueNames()
        {
            var someInvalidNames = new List<string> { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "+", "=" };
            foreach (var name in someInvalidNames)
            {
                yield return new object [] { name };
            }
        }

        [Fact]
        public void CreateQueue_WithName81CharsLong_ShouldThrowAmazonSQSException()
        {
            var sqsRequest = new CreateQueueRequest();
            sqsRequest.QueueName = new string('A', 81);

            var exception = Assert.Throws<AmazonSQSException>(() => _sqs.CreateQueue(sqsRequest));
            Assert.Contains("InvalidParameterValue", exception.ErrorCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, exception.StatusCode);
        }


        [Fact]
        public void CreateTwoQueues_WithDifferentQueueNameCapitalisations_ShouldCreateTwoQueues()
        {
            const string queueName1 = "QUEUE-NAME";
            const string queueName2 = "queue-name";

            var sqsRequest1 = new CreateQueueRequest(queueName1);
            var sqsRequest2 = new CreateQueueRequest(queueName2);

            var response1 = _sqs.CreateQueue(sqsRequest1);
            var response2 = _sqs.CreateQueue(sqsRequest2);

            Assert.True(response1.HttpStatusCode == System.Net.HttpStatusCode.OK);
            Assert.EndsWith(queueName1, response1.QueueUrl);

            Assert.True(response2.HttpStatusCode == System.Net.HttpStatusCode.OK);
            Assert.EndsWith(queueName2, response2.QueueUrl);

            // Find the the queue in the list of queues
            ListQueuesRequest listQueuesRequest = new ListQueuesRequest();
            ListQueuesResponse listQueuesResponse = _sqs.ListQueues(listQueuesRequest);

            var createdQueue = listQueuesResponse.QueueUrls.Where(q => q.EndsWith(queueName1) || q.EndsWith(queueName2));
            Assert.True(createdQueue.Count() == 2);

            // Cleanup
            SqsQueueUtils.DeleteQueue(response1.QueueUrl, _sqs);
            SqsQueueUtils.DeleteQueue(response2.QueueUrl, _sqs);
        }

        [Fact]
        public void CreateExistingQueue_WithSameQueueName_ShouldReturnsUrlOfExistingQueue()
        {
            const string queueName = "SameQueueName";

            var sqsRequest1 = new CreateQueueRequest(queueName);
            var sqsRequest2 = new CreateQueueRequest(queueName);

            var response1 = _sqs.CreateQueue(sqsRequest1);
            var response2 = _sqs.CreateQueue(sqsRequest2);

            Assert.True(response1.HttpStatusCode == System.Net.HttpStatusCode.OK);
            Assert.EndsWith(queueName, response1.QueueUrl);

            Assert.True(response2.HttpStatusCode == System.Net.HttpStatusCode.OK);
            Assert.EndsWith(queueName, response2.QueueUrl);

            // Cleanup
            SqsQueueUtils.DeleteQueue(response1.QueueUrl, _sqs);
        }

        /// <summary>
        /// Note: Elastic MQ does NOT appear to support FIFO queues at this time and creating a FIFO queue fails.
        /// https://github.com/adamw/elasticmq/issues/92
        /// </summary>
        public void CreateFifoQueue_WithFifoSuffix_ShouldCreateFifoQueue()
        {
            var attributes = new Dictionary<string, string>();
            attributes.Add(QueueAttributeName.FifoQueue, "true");
            attributes.Add(QueueAttributeName.ContentBasedDeduplication, "true");

            var sqsRequest = new CreateQueueRequest()
            {
                QueueName = "FifoQueue.fifo",
                Attributes = attributes
            };

            // Will Fail if using Elastic MQ.
            var response1 = _sqs.CreateQueue(sqsRequest);
        }
    }
}
