using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace PowerApps.Samples {

  class Program {
    //Get configuration data from App.config connectionStrings
    static readonly string connectionString = ConfigurationManager.ConnectionStrings["Connect"].ConnectionString;
    static readonly ServiceConfig serviceConfig = new ServiceConfig(connectionString);
    //Controls the max degree of parallelism
    static readonly int maxDegreeOfParallelism = 10;
    //Source Projects to simulate migration for.
    static readonly int numberOfProjects = 100;
    //Number of tasks to simulate per source project.
    static readonly int numberOfTasksPerProject = 10;
    //Number of team members per project.  NOTE Each task will have 2 resource assignments created as part of the migration
    static readonly int numberOfTeamMembers = 10;
    //Simulate the use of parent tasks.  When true then 10 tasks will be added to each parent task.
    static readonly bool useParentTasks = true;
    //Simulate the use of dependencies between tasks.  This will create dependencies for the child tasks and between parent tasks if using parent tasks
    //TODO: This logic is pending.
    static readonly bool useDependency = true;

    static void Main(string[] args) {
      #region Optimize Connection

      //Change max connections from .NET to a remote service default: 2
      System.Net.ServicePointManager.DefaultConnectionLimit = 65000;
      //Bump up the min threads reserved for this app to ramp connections faster - minWorkerThreads defaults to 4, minIOCP defaults to 4 
      System.Threading.ThreadPool.SetMinThreads(100, 100);
      //Turn off the Expect 100 to continue message - 'true' will cause the caller to wait until it round-trip confirms a connection to the server 
      System.Net.ServicePointManager.Expect100Continue = false;
      //Can decreas overall transmission overhead but can cause delay in data packet arrival
      System.Net.ServicePointManager.UseNagleAlgorithm = false;

      #endregion Optimize Connection

      var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

      // First we will simulate getting a list of source projects, project tasks, team members, resource assignments.
      // This is going to be just a collection we build in memory.  You will be retrieving source data as needed.
      List<ScheduleAPIDataMigrationHelpers.SourceProject> sourceProjects = ScheduleAPIDataMigrationHelpers.GetSourceProjects(numberOfProjects, numberOfTasksPerProject, numberOfTeamMembers, useParentTasks, useDependency);

      // No load up the existing projects
      ConcurrentBag<ScheduleAPIDataMigrationHelpers.ExistingProject> _existingProjects = new ConcurrentBag<ScheduleAPIDataMigrationHelpers.ExistingProject>();
      
    }
  }
}
