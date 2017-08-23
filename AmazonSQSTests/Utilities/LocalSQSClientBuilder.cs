using Amazon.Runtime;
using Amazon.SQS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonSQS.Tests.API.Utilities
{
    public class LocalSQSClientBuilder
    {
        public static AmazonSQSClient CreateClient()
        {
            var url = "http://localhost:9324";
            var amazonSQSConfig = new AmazonSQSConfig();
            amazonSQSConfig.ServiceURL = url;
            return new AmazonSQSClient(new BasicAWSCredentials("x", "x"), amazonSQSConfig);
        }
    }
}
