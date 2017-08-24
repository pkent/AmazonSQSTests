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
using System.Net;

namespace AmazonSQS.Tests.API
{
    public class SQSCoreEndToEndTests : SQSTestBase
    {
        /// <summary>
        /// The purpose is to do a simple end to end positive test to make sure core SQS Queue functionality is working.
        /// This includes creating a standard queue, sending a message, receiving a message and deleting it afterwords.
        /// </summary>
        [Fact]
        public void EndToEndCreateQueueSendMessageReceiveMessageDeleteMessage_WithMessageAttributes_ShouldHaveEmptyQueueAfterDeleting()
        {
            const string queueName = "StandardQueue-CoreEndToEndTest";

            var createQueueResponse = CreateQueue(queueName);
            ValidateQueueCreated(createQueueResponse, queueName);

            var message = new SqsMessage("This is a simple message");
            var messageResponse = SendMessage(message);
            ValidateMessageSent(messageResponse);

            var receiveMessageResponse = ReceiveMessage();

            ValidateMessageReceived(receiveMessageResponse, message);

            DeleteMessageFromQueue(receiveMessageResponse);

            ValidateMessageDeletedFromQueue();
        }

        private CreateQueueResponse CreateQueue(string queueName)
        {
            var sqsRequest = new CreateQueueRequest(queueName);
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;

            return createQueueResponse;
        }

        private void ValidateQueueCreated(CreateQueueResponse createQueueResponse, string queueName)
        {
            Assert.True(createQueueResponse.HttpStatusCode == HttpStatusCode.OK);
            Assert.True(_queueUrl.EndsWith($"/{queueName}"));

            var listQueuesRequest = new ListQueuesRequest();
            var listQueuesResponse = _sqs.ListQueues(listQueuesRequest);

            var createdQueue = listQueuesResponse.QueueUrls.Where(q => q.EndsWith(queueName));
            Assert.True(createdQueue.Count() == 1);
        }

        private SendMessageResponse SendMessage(SqsMessage message)
        {
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = message.Body,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "Custom", new MessageAttributeValue { DataType = "String", StringValue = "Custom Data" } }
                }
            };
            return _sqs.SendMessage(sendMessageRequest);
        }

        private void ValidateMessageSent(SendMessageResponse messageResponse)
        {
            Assert.True(messageResponse.HttpStatusCode == HttpStatusCode.OK);
        }

        private ReceiveMessageResponse ReceiveMessage()
        {
            var receiveMessageRequest = new ReceiveMessageRequest()
            {
                QueueUrl = _queueUrl,
                WaitTimeSeconds = 10,
                MessageAttributeNames = new List<string>() { "Custom" }
            };
            return _sqs.ReceiveMessage(receiveMessageRequest);
        }

        private void ValidateMessageReceived(ReceiveMessageResponse receiveMessageResponse, SqsMessage message)
        {
            Assert.True(receiveMessageResponse.HttpStatusCode == HttpStatusCode.OK);
            Assert.True(receiveMessageResponse.Messages.Count == 1);
            var receivedMessage = receiveMessageResponse.Messages.First();
            Assert.True(receivedMessage.Body == message.Body);
            Assert.True(receivedMessage.MD5OfBody == message.Md5Hash);
            Assert.True(receivedMessage.MessageAttributes.Count() == 1);
            Assert.True(receivedMessage.MessageAttributes.First().Value.StringValue == "Custom Data");
        }

        private void DeleteMessageFromQueue(ReceiveMessageResponse receiveMessageResponse)
        {
            var messageReceiptHandle = receiveMessageResponse.Messages.First().ReceiptHandle;
            var deleteRequest = new DeleteMessageRequest();
            deleteRequest.QueueUrl = _queueUrl;
            deleteRequest.ReceiptHandle = messageReceiptHandle;
            _sqs.DeleteMessage(deleteRequest);
        }

        private void ValidateMessageDeletedFromQueue()
        {
            var receiveEmptyMessageRequest = new ReceiveMessageRequest();
            receiveEmptyMessageRequest.QueueUrl = _queueUrl;
            var receiveEmptyMessageResponse = _sqs.ReceiveMessage(receiveEmptyMessageRequest);
            Assert.Empty(receiveEmptyMessageResponse.Messages);
        }
    }
}
