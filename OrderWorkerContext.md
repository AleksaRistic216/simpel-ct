## Order Worker Context

You have been assigned to investigate a data integrity issue in our Order Processing Service. This is a C# .NET 10 background worker that consumes messages from an SQS queue and interacts with our RDS database.

We have provided a simplified version of the production code. While it uses an InMemory database and a MockQueue, you should treat the logic as if it were interacting with actual AWS services.

### Incident Report: "The Price Mismatch"

We have a major issue with Order #1001. An administrator updated the Price of this order directly in the system to correct a data entry error. 
However, even after the change was confirmed in the database, the background worker processed the order using the original, incorrect price.

We need to know why the worker ignored the updated database record and how to ensure this never happens again.

Analyze the provided solution, specifically OrderWorker.cs.

Find the Root Cause: Why did the worker use the old price even though the database was updated externally?

**The "What Else":** While investigating the price bug, did you notice any other technical issues or inefficiencies that would impact production stability or costs?

### What we expect from your solution:

- **Reproduce the Issue**: Ensure you can reproduce the price mismatch issue locally using the provided code.
  - Locally just running it produces multiple problems, not related even to wrong value, including false positive "Processed" even tho it didn't find order cuz of wrong id passed in queue message 
- **Analyze the Code**: Carefully review OrderWorker.cs to understand how it interacts with the database and the message queue.
  - It is background service, checking queue for new messages, processing them one by one (a bit strange)
- **Identify the Root Cause**: Determine why the worker processed the order with the old price despite the database update.
  - Well, it didn't process at all. It did give false "Processed" as it was fallback (which is really bad, to have Success as fallback). Root of it was hardcoded id which was wrong in message (null order)
- **Propose a Solution**: Suggest a fix to ensure the worker always uses the latest price from the database.
  - Done, along with few improvements and other fixes (like that infinite queue on pending state)
- **Document Findings**: Write a detailed report explaining your findings, the root cause, and the proposed solution.
  - Did above

#### Bonus: Brief written answers to the Cloud Architecture questions below
- **Bonus 1**: If we scale to 500+ instances of this worker, how do we protect our RDS database from connection exhaustion?
  - RDS Proxy - put between service and RDS. Will limit concurrency
- **Bonus 2**: If a message fails to process due to an error, how should we handle it to prevent it from blocking the rest of the queue?
  - Done that in code. Catch, re-queue to max tries, after failing all that - log details and update status to failed
- **Bonus 3**: How do we ensure a customer isn't charged twice if S3/SQS delivers the same message more than once?
  - This is multi level. First would be to use SQS feature FIFO on Id (https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/FIFO-queues-exactly-once-processing.html) handling 5min window.
  - Then in code we would implement new status "In process" and update order once we start working on it, falling back to pending/failed/completed. With this we will handle post 5min window.
  - additional option if we are not using SQS FIFO (not sure if appropriate these days): new table containing only processed orders, with primary key like
  "sqs-something-bla-bla-order-{id}", and on race condition, first one will process, insert here, and update order, however second
  one will break on inserting in this table as primary key already exist, falling back, and not updating order (or rolling back if complex transaction)
