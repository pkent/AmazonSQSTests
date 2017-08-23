Amazon SQS API Tests
====================

Author: Paul Kent
Date: 24/8/2017

The Visual Studio solution contains API tests that test some of the basic SQS APIs using the xUnit framework.

Test case code convention:
--------------------------
Test cases are essentially methods encapulated in clases which by convention ends with the word "Tests". For example the class "SQSCreateQueueTests" contains test cases related to creating queues.

Each test cases is a public method annotated with the [Fact] attribute as is required for xUnit to recognise this as a Test. The test case names are generally descriptive such as "CreateStandardQueue_WithValidQueueName_ShouldCreateQueue".
There are three parts each separated with an underscore. The first part is what the test does, the second is what parameters or conditions will be used and the third is the expected outcome of the test.

Methodology
-----------
While reading the documentation on the APIs I tried to understand the what a developer would be trying to do with these the APIs and I started with a positive test case such as the one in the SQSCoreEndToEndTests class that covers basic end to end queue functionality. I then looked at what the parameters for the API calls where and what are considered some invalid values such as invalid queue names and wrote some negative test cases for those based on the API documentation. 

I've designed the test cases so each one is completely independant from another and each test case creates all necessary data it needs to run. This makes it easier to run in parallel.

The test cases deal only with standard queues and not FIFO queues as I found out Elastic MQ doesn't seem to support FIFO queues. I've also noted that Elastic MQ seems to also have some differences with RedrivePolicy Attribute values which it doesn't support according to the console logs.

The test cases are certainly not an exhaustive list and they don't cover every optional attribute and I didn't complete all the test for the extended functionality like PurgeQueue and ChangeMessageVisibility.

Interestingly in one of my negative test cases (SendMessage_WithoutBody_ShouldThrowException) I found in Elastic MQ where if a message does not contain a body, an internal server error occurs instead of handling it gracefully. Not a major issue but interesting.

Known failing Tests
-------------
There are two failing tests most likely as a result of Elastic MQ. These are:
- SendMessage_WithoutBody_ShouldThrowException
- ReceiveMessageMultipleTimes_UsingDeadLetterQueue_MessageShouldBePlacedInDeadLetterQueue

I haven't had the chance to test these with Amazon to confirm if there is a difference in behaviour but I suspect there would be be and I would expect these test to pass on Amazon.