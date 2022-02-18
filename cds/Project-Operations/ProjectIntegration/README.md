# Data Migration / Data Integration using Project Operations ScheduleAPIs 

This sample shows how to use parallel operations to load Project Operations
entities which require use of the ScheduleAPIs.

## Note

If you want to use Fiddler to observe the expected service protection API limits, you will need to set the number of records to create to be around 10,000. They will start to appear after 5 minutes. 

Note how the application retries the failures and completes the flow of all the records.

## More information

For more information, see 

- [Dataflow (Task Parallel Library)](https://docs.microsoft.com/dotnet/standard/parallel-programming/dataflow-task-parallel-library)
- [Task Parallel Library (TPL)](https://docs.microsoft.com/dotnet/standard/parallel-programming/task-parallel-library-tpl)
- [Parallel Programming in .NET](https://docs.microsoft.com/dotnet/standard/parallel-programming/)