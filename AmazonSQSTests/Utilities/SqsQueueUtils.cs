using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonSQS.Tests.API.Utilities
{
    public class SqsQueueUtils
    {
        public static string CreateQueueNameWithRandomSuffix(string queuePrefix)
        {
            return queuePrefix + new Random().Next(10000);
        }

        public static void DeleteQueue(string queueUrl, AmazonSQSClient sqsClient)
        {
            sqsClient.DeleteQueue(new DeleteQueueRequest(queueUrl));
        }
    }
}
