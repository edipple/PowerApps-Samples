﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PowerApps.Samples {
  class Program {
    //Get configuration data from App.config connectionStrings
    static readonly string connectionString = ConfigurationManager.ConnectionStrings["Connect"].ConnectionString;
    static readonly ServiceConfig serviceConfig = new ServiceConfig(connectionString);
    //Controls the max degree of parallelism
    static readonly int maxDegreeOfParallelism = 10;
    //Total number of projects to create
    static readonly int numberOfProjectsToCreate = 1000;
    //Number of tasks per project.
    static readonly int numberOfTasksPerProject = 10;
    //Number of assignments per task
    static readonly int numberOfAssignmentsPerTask = 3;

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

      var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions {
        MaxDegreeOfParallelism = maxDegreeOfParallelism
      };

      var count = 0;
      double secondsToComplete;

      // This is not using the preferred AsyncParallelOperations model.  The goals is to allow you to
      // see how the individual calls to the ScheduleAPIs are processed so we will use a simple threading
      // model to run 10 threads in parallel.  Only using 1 systemuser account for authentication so at max
      // we can have 10 operationsets processing at a time.  Thus 10 threads where each thread will execute
      // the operationsets synchronously.  While each thread is running synchronously have 10 threads allows
      // the execution of 10 operationsets in parallel.


    }
  }
}
