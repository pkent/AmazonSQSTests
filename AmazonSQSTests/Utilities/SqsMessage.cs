using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonSQS.Tests.API.Utilities
{
    public class SqsMessage
    {
        public string Body { get; private set; }
        public string Md5Hash { get; private set; }

        public SqsMessage(string body)
        {
            Body = body;
            Md5Hash = CreateMD5Hash(body);
        }

        private string CreateMD5Hash(string message)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();

            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2").ToLower());
            }

            return sb.ToString();
        }
    }
}
