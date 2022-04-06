using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;

namespace PowerApps.Samples {
  public partial class SampleProgram {
    static void Main(string[] args) {
      CrmServiceClient service = null;

      

      try {
        service = SampleHelpers.Connect("Connect");
        if (service.IsReady) {
          service.EnableAffinityCookie = false; // disable the affinity token so requests are distributed across web servers.

          // Simulate loading the source projects that need to be migrated.  This is just simulating this step.
          List<ScheduleAPILoadTestHelpers.SourceProject> sourceProjects = ScheduleAPILoadTestHelpers.GetSourceProjects(10);


          // For this sample I am also showing support for a migration scenario where you might be
          // migrating a delta of data where some of the source data was migrated in a previous run and
          // we now need to do an update if the record exists.  The challenge here is that the ScheduleAPIs
          // do not support an UPSERT concept.  So we need to take on the added work of checking if the record
          // already exists in Dynamics.
          //
          // In a production scenario you should have your Dynamics data sync to an Azure data lake and query the
          // data lake to determine what source data requires an Update versus Create.  For this sample I will
          // take the hit of checking Dynamics directly (very ineficient way of doing this).
          List<ScheduleAPILoadTestHelpers.ExistingProject> existingProjects = ScheduleAPILoadTestHelpers.LoadExistingProjects(service, sourceProjects);
          
          // This is where the focus of this sample starts
          // First we are going to create missing projects.
          foreach (ScheduleAPILoadTestHelpers.SourceProject srcProject in sourceProjects) {
            //if (!)
          }
        }
        else {
          const string UNABLE_TO_LOGIN_ERROR = "Unable to Login to Microsoft Dataverse";
          if (service.LastCrmError.Equals(UNABLE_TO_LOGIN_ERROR)) {
            Console.WriteLine("Check the connection string values in cds/App.config.");
            throw new Exception(service.LastCrmError);
          }
          else {
            throw service.LastCrmException;
          }
        }
      }
      catch (Exception ex) {
        SampleHelpers.HandleException(ex);
      }

      finally {
        if (service != null)
          service.Dispose();

        Console.WriteLine("Press <Enter> to exit.");
        Console.ReadLine();
      }


    }
  }
}
