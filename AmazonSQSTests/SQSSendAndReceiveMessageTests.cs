using Amazon.SQS;
using Amazon.SQS.Model;
using AmazonSQS.Tests.API.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AmazonSQS.Tests.API
{
    public class SQSSendAndReceiveMessageTests : SQSTestBase
    {
        /// <summary>
        /// The purpose of this test case it to attempt to simulate concurrent producers by creating multiple 
        /// tasks to send multiple messages to the same queue and to receive them while messages are being sent.
        /// All messages that are sent should get received.
        /// </summary>
        [Fact]
        public void SendMultipleMessagesConcurrentlyToSameQueue_ShouldBeAbleToReceiveAllMessagesSuccessfully()
        {
            const string queueName = "StandardQueue-MultipleMessagesTest";
            const int totalMessagesToBeSent = 20;

            _queueUrl = CreateQueue(queueName);
            var messages = CreateDummyMessagesToSend(totalMessagesToBeSent);

            SendAllMessagesConcurrently(messages, _queueUrl);

            ReceiveAllMessages(messages.ToList(), _queueUrl);

            // We should not have any further messages if we make a call to ReceiveMessage
            var receiveEmptyMessageRequest = new ReceiveMessageRequest(_queueUrl);
            var receiveEmptyMessageResponse = _sqs.ReceiveMessage(receiveEmptyMessageRequest);
            Assert.Empty(receiveEmptyMessageResponse.Messages);
        }

        private string CreateQueue(string queueName)
        {
            var sqsRequest = new CreateQueueRequest(queueName);
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            return createQueueResponse.QueueUrl;
        }

        private IList<SqsMessage> CreateDummyMessagesToSend(int totalMessagesToBeSent)
        {
            return Enumerable.Range(0, totalMessagesToBeSent).Select((i, r) => new SqsMessage("Message" + i)).ToList();
        }

        private void SendAllMessagesConcurrently(IEnumerable<SqsMessage> messages, string queueUrl)
        {
            foreach (var message in messages)
            {
                Task.Run(() =>
                {
                    var sendMessageRequest = new SendMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MessageBody = message.Body
                    };

                    _sqs.SendMessage(sendMessageRequest);
                });
            }
        }

        private void ReceiveAllMessages(IList<SqsMessage> messages, string queueUrl)
        {
            bool hasMessagesToBeReceived = true;
            int numberOfReceiveMessageAttempts = 0;
            int totalMessagesToReceive = messages.Count();

            // Start receiving all messages
            while (hasMessagesToBeReceived)
            {
                var receiveMessageRequest = new ReceiveMessageRequest(queueUrl);
                var receiveMessageResponse = _sqs.ReceiveMessage(receiveMessageRequest);
                numberOfReceiveMessageAttempts++;

                var receivedMessage = receiveMessageResponse.Messages.FirstOrDefault();

                // It is possible that a call to ReceiveMessage using Short Polling (the default) does not return a message 
                // and and requires a subsequent ReceiveMessage call.
                if (receivedMessage == null)
                {
                    continue;
                }

                // The order of the received messages is not guaranteed when we are not using a FIFO queue
                // however message received should be one of the messages that we originally sent.
                var message = messages.Where(m => m.Md5Hash == receivedMessage.MD5OfBody).SingleOrDefault();

                Assert.NotNull(message);

                var deleteRequest = new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = receivedMessage.ReceiptHandle
                };

                _sqs.DeleteMessage(deleteRequest);
                messages.Remove(message);

                if (numberOfReceiveMessageAttempts > totalMessagesToReceive * 10)
                {
                    throw new Exception($"ReceiveMessage hasn't received all messages {totalMessagesToReceive} even after {totalMessagesToReceive * 10} requests."
                        + $"{messages.Count()} out of a total of {totalMessagesToReceive} were received.");
                }
                if (messages.Count() == 0)
                {
                    hasMessagesToBeReceived = false;
                }
            }

            // Once all messages are received the messages list should be empty as each message is removed
            // from our list once we receive it from the queue.
            Assert.Empty(messages);
        }

        /// <summary>
        /// Certain unicode characters are not acceptable in the body of a message. This tests one of those
        /// to make sure it gets rejected.
        /// 
        /// See: http://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessage.html
        /// </summary>
        [Fact]
        public void SendMessage_WithInvalidContentBody_ShouldThrowException()
        {
            const string queueName = "StandardQueue-InvalidContentTest";

            var sqsRequest = new CreateQueueRequest(queueName);
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = new string(new char[] { '\u0019' })
            };

            var exception = Assert.Throws<InvalidMessageContentsException>(() => _sqs.SendMessage(sendMessageRequest));
            Assert.Contains("InvalidMessageContents", exception.ErrorCode);
        }

        /// <summary>
        /// NOTE: This test crashes Elastic MQ instead of returning gracefully!
        /// java.util.NoSuchElementException: key not found: MessageBody
        /// </summary>
        [Fact]
        public void SendMessage_WithoutBody_ShouldThrowException()
        {
            const string queueName = "StandardQueue-EmptyMessageBodyTest";

            var sqsRequest = new CreateQueueRequest(queueName);
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl
            };

            var exception = Assert.Throws<InvalidMessageContentsException>(() => _sqs.SendMessage(sendMessageRequest));
            Assert.Contains("InvalidMessageContents", exception.ErrorCode);
        }

        [Fact]
        public void SendMessage_WithBodyLargerThan256K_ShouldThrowException()
        {
            const string queueName = "StandardQueue-MessageBodyLargerThan256k";

            var sqsRequest = new CreateQueueRequest(queueName);
            var createQueueResponse = _sqs.CreateQueue(sqsRequest);
            _queueUrl = createQueueResponse.QueueUrl;

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = new string('A', (256 * 1024) + 1)
            };

            var exception = Assert.Throws<AmazonSQSException>(() => _sqs.SendMessage(sendMessageRequest));
            Assert.Contains("MessageTooLong", exception.ErrorCode);
        }
    }
}
