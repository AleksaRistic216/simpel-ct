## High-Performance Data Modeling

### The Scenario
Our system handles thousands of companies. We have moved away from Relational databases to DynamoDB to handle massive scale. However, we need to satisfy a complex set of business requirements using Single Table Design.

You must design a schema for a single DynamoDB table that supports the following access patterns efficiently (using Query, never Scan).

### Main Query

Given a CompanyID, fetch the current Company metadata, the full history of metadata changes (versions), and all Users belonging to that company, all in one single network request.

### Complications
- Users can log in using their email. We need to find a User record by their Email across the entire system (all companies combined) instantly.
- Some companies have 50,000 users. How do you ensure main query doesn't fail or time out due to the 1MB response limit in DynamoDB?

---

### Tasks

#### 1. Schema Design
Propose the Partition Key (PK) and Sort Key (SK) values for the following entities:
- **Company Metadata (Current)**: What are the PK/SK?
- **Company Version History (Audit Trail)**: How do you store previous versions?
- **User Records**: What are the PK/SK?

#### 2. Versioning
If an admin updates the company name, we need to preserve the old name for legal audit.
- How do you store the v1, v2, v3 records?
- When performing main query, how do you ensure the "Current/Latest" version is easy to identify among the history?

#### 3. Global Secondary Index (GSI)
- Explain how you would design a GSI to satisfy lookup by email complication.
- What would be the PK and SK of this index?

#### 4. Implementation
- Describe the parameters for your Query call for main query.
- How do you use the Sort Key to ensure Users and Metadata come back together?
- How would you handle Pagination if a company has thousands of users?
- Describe the KeyConditionExpression and ExpressionAttributeValues required to pull the "Item Collection."
- You may use the standard AWS SDK or high-performance alternatives (e.g., EfficientDynamoDB). Regardless of the tool, explain how you leverage the Partition Key and Sort Key to avoid multiple round-trips to the database.

This isn't easy at all... Lets try thinking of high level only...

Second rewrite... For PK I would go with `COMPANY#<id>` and under SK I would put company data (for specific version)
and users. With that, we would have SKs something like this: `DATA#CURRENT`, `DATA#VERSION#001`, `DATA#VERSION#002`, `USER#<id>`, `USER#<id>`

This will allow us to easily persist versions, but keep them separate for fast quering. When new version comes, copy current to
DATA#VERSION#<next> with old data, and update new data in current.

Regarding email lookup over multiple partitions, GSI with PK on users email will unify all users. 

Now lets answer questions:

 - Describe the parameters for your Query call for main query.
   - Query over PK with `COMPANY#{id}`. Will get all company data along with its users in single query
 - How do you use the Sort Key to ensure Users and Metadata come back together?
   - I am not sure I do. I mean, I put them under same partition (`COMPANY#{id}`). Only thing I ensured (by "luck") is
   - that DATA comes before USER, therefore I will always retreive company data in first call (if quering only by PK and it has a lot of users and exceeds limit)
 - How would you handle Pagination if a company has thousands of users?
   - Checking last evaluated key. If not null, read again by passing start key (last evaluated one)
 - Describe the KeyConditionExpression and ExpressionAttributeValues required to pull the "Item Collection."
   - KeyConditionExpression - tells how to get (for example match only PK or match PK and SK (by some condition, fot example to get only data or only users under company))
   - ExpressionAttributeValues - passing actual values for KeyConditionExpression
 - You may use the standard AWS SDK or high-performance alternatives (e.g., EfficientDynamoDB). Regardless of the tool, explain how you leverage the Partition Key and Sort Key to avoid multiple round-trips to the database.
   - Only time I would go multiple times is if I fetch only by PK (returns data + all users which is thousands which makes)
   it paginated. To solve this, I can match things by SK too, altho again will have same problem if fetching thousands users, but will
   solve it if fetching only company details