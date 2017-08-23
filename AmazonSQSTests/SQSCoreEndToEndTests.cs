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
    public class SQSCoreEndToEndTests : SQSTestBase
    {
        /// <summary>
        /// The purpose is to do a simple end to end positive test to make sure core SQS Queue functionality is working.
        /// This includes creating a standard queue, sending a message, receiving a message and deleting it afterwords.
        /// </summary>
        [Fact]
        public void EndToEndCreateQueueSendMessageReceiveMessageDeleteMessage_WithMessageAttributes_ShouldHaveEmptyQueueAfterDeleting()
        {
            string queueName = "StandardQueue-CoreEndToEndTest";

            var sqsRequest = new CreateQueueRequest();
            sqsRequest.QueueName = queueName;

            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;

            // Ensure queue is created correctly
            Assert.True(createQueueResponse.HttpStatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(_queueUrl.EndsWith($"/{queueName}"));

            var message = new SqsMessage("This is a simple message");
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = message.Body,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "Custom", new MessageAttributeValue { DataType = "String", StringValue = "Custom Data" } }
                }
            };
            var messageResponse = _sqs.SendMessage(sendMessageRequest);

            List<string> AttributesList = new List<string>();
            AttributesList.Add("Custom");

            var receiveMessageRequest = new ReceiveMessageRequest();
            receiveMessageRequest.QueueUrl = _queueUrl;
            receiveMessageRequest.WaitTimeSeconds = 10;
            receiveMessageRequest.MessageAttributeNames = AttributesList;
            var receiveMessageResponse = _sqs.ReceiveMessage(receiveMessageRequest);

            // Ensure message is received
            Assert.True(receiveMessageResponse.Messages.Count == 1);
            var receivedMessage = receiveMessageResponse.Messages.First();
            Assert.True(receivedMessage.Body == message.Body);
            Assert.True(receivedMessage.MD5OfBody == message.Md5Hash);
            Assert.True(receivedMessage.MessageAttributes.Count() == 1);
            Assert.True(receivedMessage.MessageAttributes.First().Value.StringValue == "Custom Data");

            var messageReceiptHandle = receiveMessageResponse.Messages.First().ReceiptHandle;
            var deleteRequest = new DeleteMessageRequest();
            deleteRequest.QueueUrl = _queueUrl;
            deleteRequest.ReceiptHandle = messageReceiptHandle;
            _sqs.DeleteMessage(deleteRequest);

            // Ensure message was deleted from queue
            var receiveEmptyMessageRequest = new ReceiveMessageRequest();
            receiveEmptyMessageRequest.QueueUrl = _queueUrl;
            var receiveEmptyMessageResponse = _sqs.ReceiveMessage(receiveEmptyMessageRequest);
            Assert.Empty(receiveEmptyMessageResponse.Messages);
        }
    }
}
