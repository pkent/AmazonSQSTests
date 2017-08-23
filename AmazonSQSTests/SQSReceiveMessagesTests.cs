using Amazon.SQS;
using Amazon.SQS.Model;
using AmazonSQS.Tests.API.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AmazonSQS.Tests.API
{
    public class SQSReceiveMessagesTests : SQSTestBase
    {
        /// <summary>
        /// Note: For standard queues, the visibility timeout isn't a guarantee against receiving a message twice.
        /// See: http://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html
        /// So this test while it does work isn't entirely valid with standard queues.
        /// </summary>
        [Fact]
        public void ReceiveMessage_WithVisibilityTimeout_ShouldPreventConsumerFromRetrievingMessageDuringTheVisibilityTimeout()
        {
            const string queueName = "MessageVisibility_StandardQueue";
            const int visibilityTimeoutSeconds = 15;
            var message = new SqsMessage("Test");

            var sqsRequest = new CreateQueueRequest(queueName)
            {
                Attributes = new Dictionary<string, string>()
                {
                    { QueueAttributeName.VisibilityTimeout , visibilityTimeoutSeconds.ToString() }//,
                    //{ QueueAttributeName.ReceiveMessageWaitTimeSeconds, "5" }
                }
            };
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;
            var response = _sqs.SendMessage(new SendMessageRequest(_queueUrl, message.Body));

            var receiveMessageRequest = new ReceiveMessageRequest()
            {
                QueueUrl = _queueUrl
            };

            var receiveMessageResponse = _sqs.ReceiveMessage(receiveMessageRequest);
            Assert.Equal(message.Body, receiveMessageResponse.Messages.First().Body);

            var secondSqsClient = LocalSQSClientBuilder.CreateClient();
            var receiveMessageResponseWithinTimeout = secondSqsClient.ReceiveMessage(_queueUrl);
            Assert.Empty(receiveMessageResponseWithinTimeout.Messages);

            Thread.Sleep((visibilityTimeoutSeconds) * 1000);

            var receiveMessageResponseAfterTimeout = secondSqsClient.ReceiveMessage(_queueUrl);
            Assert.Equal(message.Body, receiveMessageResponseAfterTimeout.Messages.First().Body);
        }

        [Fact]
        public void ReceiveMessage_WithWaitTimeSet_ShouldEnableLongPolling()
        {
            const string queueName = "MessageWaitTime_StandardQueue";
            const int waitTimeSeconds = 10;
            var message = new SqsMessage("Test");

            var sqsRequest = new CreateQueueRequest(queueName);
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;

            var secondSqsClient = LocalSQSClientBuilder.CreateClient();
            var receiveMessageRequest = new ReceiveMessageRequest()
            {
                QueueUrl = _queueUrl,
                WaitTimeSeconds = waitTimeSeconds
            };

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var receiveMessageResponseWithinTimeout = secondSqsClient.ReceiveMessage(receiveMessageRequest);
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds < (waitTimeSeconds * 1000) + 1000);
            Assert.Empty(receiveMessageResponseWithinTimeout.Messages);

            var response = _sqs.SendMessage(new SendMessageRequest(_queueUrl, message.Body));

            stopwatch.Reset();
            stopwatch.Start();

            var receiveMessageResponseAfterTimeout = secondSqsClient.ReceiveMessage(_queueUrl);
            stopwatch.Stop();

            // The ReceiveMessage call should be quick as there is a message in the queue to receive
            Assert.True(stopwatch.ElapsedMilliseconds < waitTimeSeconds * 1000);
            Assert.Equal(message.Body, receiveMessageResponseAfterTimeout.Messages.First().Body);
        }

        /// <summary>
        /// This test doesn't appear to work as expected with Elastic MQ. The console states that "RedrivePolicy" is 
        /// not supported by Elastic MQ.
        /// </summary>
        [Fact]
        public void ReceiveMessageMultipleTimes_UsingDeadLetterQueue_MessageShouldBePlacedInDeadLetterQueue()
        {
            const string sourceQueueName = "SourceQueue_RedrivePolicy_StandardQueue";
            const string deadLetterQueueName = "DeadLetterQueue_StandardQueue";
            var message = new SqsMessage("Test");

            var createQueueResponse = _sqs.CreateQueue(sourceQueueName);
            var deadLetterQueueResponse = _sqs.CreateQueue(deadLetterQueueName);

            var queueAttrs = _sqs.GetQueueAttributes(new GetQueueAttributesRequest(deadLetterQueueResponse.QueueUrl, new List<string> { "QueueArn" }));

            var sqsRequest = new SetQueueAttributesRequest()
            {
                Attributes = new Dictionary<string, string>()
                {
                    { QueueAttributeName.RedrivePolicy, "{\"maxReceiveCount\":\"5\", \"deadLetterTargetArn\":\"" + queueAttrs.QueueARN + "\"}" }
                },
                QueueUrl = createQueueResponse.QueueUrl

            };
            _sqs.SetQueueAttributes(sqsRequest);


            _queueUrl = createQueueResponse.QueueUrl;

            // Send a message for the source queue
            var response = _sqs.SendMessage(new SendMessageRequest(_queueUrl, message.Body));

            var receiveMessageRequest = new ReceiveMessageRequest()
            {
                QueueUrl = _queueUrl
            };

            // Read from the source queue multiple times
            for (int i = 0; i < 6; i++)
            {
                var receiveMessageResponse = _sqs.ReceiveMessage(receiveMessageRequest);
            }

            var deadLetterQueueMessageResponse = _sqs.ReceiveMessage(deadLetterQueueResponse.QueueUrl);
            
            // Verify that the message has moved to the dead-letter queue
            Assert.Equal(message.Body, deadLetterQueueMessageResponse.Messages.First().Body);

            // Cleanup
            SqsQueueUtils.DeleteQueue(deadLetterQueueResponse.QueueUrl, _sqs);
        }
    }
}
