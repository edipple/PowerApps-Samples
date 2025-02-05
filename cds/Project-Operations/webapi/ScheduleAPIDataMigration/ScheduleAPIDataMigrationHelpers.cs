﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerApps.Samples {
  public class ScheduleAPIDataMigrationHelpers {
    /*
     https://erdippleprojops.api.crm.dynamics.com/api/data/v9.2.22032.150/msdyn_projects?$select=msdyn_description,msdyn_scheduledstart,msdyn_effortestimateatcompleteeac,msdyn_projectid,msdyn_projectmanager,msdyn_taskearlieststart,             statuscode,ownerid,msdyn_workhourtemplate,msdyn_duration,msdyn_salesorderid,msdyn_calendarid,msdyn_subject,msdyn_effortcompleted,             modifiedby,createdon,msdyn_finish,msdyn_customer,msdyn_effort,statecode,msdyn_contractlineproject,msdyn_effortremaining,msftce_sourceid,modifiedon,msdyn_schedulemode,msdyn_comments,createdby,msdyn_valuestatement
&$expand=msdyn_msdyn_project_msdyn_projecttask_project($select=createdon,msdyn_requestedhours,msdyn_description,msdyn_iscritical,msdyn_effortcompleted,msdyn_parenttask,msdyn_duration,msdyn_start,msdyn_subject,msdyn_effortremaining,msdyn_scheduledend,msdyn_ismilestone,statecode,msdyn_effortestimateatcomplete,msdyn_effort,ownerid,modifiedon,      msdyn_priority,msdyn_scheduledstart,createdby,msdyn_projectbucket,modifiedby,msdyn_project,msdyn_scheduleddurationminutes,statuscode,msftce_sourceid,msdyn_finish),
&$filter=Microsoft.Dynamics.CRM.In(PropertyName=@p1,PropertyValues=@p2)
&@p1='msftce_sourceid'
&@p2=["SAMPLE-MIGRATION","SRCPRJ-01","SRCPRJ-02","SRCPRJ-03","SRCPRJ-04","SRCPRJ-05","SRCPRJ-06","SRCPRJ-07","SRCPRJ-08","SRCPRJ-09","SRCPRJ-10","SRCPRJ-11","SRCPRJ-12","SRCPRJ-13","SRCPRJ-14","SRCPRJ-15","SRCPRJ-16","SRCPRJ-17","SRCPRJ-18","SRCPRJ-19","SRCPRJ-20","SRCPRJ-21","SRCPRJ-22","SRCPRJ-23","SRCPRJ-24","SRCPRJ-25","SRCPRJ-26","SRCPRJ-27","SRCPRJ-28","SRCPRJ-29","SRCPRJ-30","SRCPRJ-31","SRCPRJ-32","SRCPRJ-33","SRCPRJ-34","SRCPRJ-35","SRCPRJ-36","SRCPRJ-37","SRCPRJ-38","SRCPRJ-39","SRCPRJ-40","SRCPRJ-41","SRCPRJ-42","SRCPRJ-43","SRCPRJ-44","SRCPRJ-45","SRCPRJ-46","SRCPRJ-47","SRCPRJ-48","SRCPRJ-49","SRCPRJ-50"]
    */

    // Sample URL: https://erdippleprojops.api.crm.dynamics.com/api/data/v9.2/msdyn_projects?  // Append the following string to the url.
    private static string existingProjectsWebApi =
      @"msdyn_projects?$select=msdyn_description,msdyn_scheduledstart,msdyn_effortestimateatcompleteeac,msdyn_projectid,msdyn_projectmanager,msdyn_taskearlieststart,statuscode,ownerid,msdyn_workhourtemplate,msdyn_duration,msdyn_salesorderid,msdyn_calendarid,msdyn_subject,msdyn_effortcompleted,modifiedby,createdon,msdyn_finish,msdyn_customer,msdyn_effort,statecode,msdyn_contractlineproject,msdyn_effortremaining,msftce_sourceid,modifiedon,msdyn_schedulemode,msdyn_comments,createdby,msdyn_valuestatement
        &$expand=msdyn_msdyn_project_msdyn_projecttask_project($select=msdyn_scheduledstart,msdyn_scheduledend,msdyn_effort,msdyn_subject,_msdyn_resourcecategorypricingdimension_value,_msdyn_projectbucket_value,_msdyn_parenttask_value,_msdyn_transactioncategory_value,_msdyn_organizationalunitpricingdimension_value),
        &$filter=Microsoft.Dynamics.CRM.In(PropertyName=@p1,PropertyValues=@p2)
        &@p1='msftce_sourceid'
        &@p2=[{SOUREIDVALUES}]";
//Sample of source values: "SRCPRJ-01","SRCPRJ-02","SRCPRJ-03","SRCPRJ-04","SRCPRJ-05","SRCPRJ-06","SRCPRJ-07","SRCPRJ-08","SRCPRJ-09","SRCPRJ-10","SRCPRJ-11","SRCPRJ-12","SRCPRJ-13","SRCPRJ-14","SRCPRJ-15","SRCPRJ-16","SRCPRJ-17","SRCPRJ-18","SRCPRJ-19","SRCPRJ-20","SRCPRJ-21","SRCPRJ-22","SRCPRJ-23","SRCPRJ-24","SRCPRJ-25","SRCPRJ-26","SRCPRJ-27","SRCPRJ-28","SRCPRJ-29","SRCPRJ-30","SRCPRJ-31","SRCPRJ-32","SRCPRJ-33","SRCPRJ-34","SRCPRJ-35","SRCPRJ-36","SRCPRJ-37","SRCPRJ-38","SRCPRJ-39","SRCPRJ-40","SRCPRJ-41","SRCPRJ-42","SRCPRJ-43","SRCPRJ-44","SRCPRJ-45","SRCPRJ-46","SRCPRJ-47","SRCPRJ-48","SRCPRJ-49","SRCPRJ-50"
    
    public static string[] BookableResourceNames = {"Abraham McCormick", "Allison Dickson", "Ashley Chinn", "Bernadette Foley", "Bob Kozak", "Brady Hannon", "Cheri Castaneda", "Christal Robles", "Christie Dawson", "Clarence Desimone" };
    
    // Load a simulated collection of source projects to process.
    public static List<SourceProject> GetSourceProjects(int projectsToProcess, int numberOfTasks, int numberOfTeamMembers, bool useParentTasks, bool useDependency) {
      List<SourceProject> _sourceProjects = new List<SourceProject>();

      for (int i = 0; i < projectsToProcess; i++) {
        _sourceProjects.Add(new SourceProject($"SRCPRJ-{i}", numberOfTasks, numberOfTeamMembers, useParentTasks, useDependency));
      }

      return _sourceProjects;
    }

    public static ConcurrentBag<ExistingTargetProject> GetExistingProjects(CDSWebApiService svc, List<SourceProject> sourceProjects) {
      ConcurrentBag<ExistingTargetProject> _existingProjects = new ConcurrentBag<ExistingTargetProject>();

      int iRetrieveMultipleCounter = 0;  // the filter in the FetchXML will retrieve projects based on a list of source projects.   So only do this for 50 source projects at a time.
      string sourceValues = "";
      for (int i = 0; i < sourceProjects.Count; i++) {
        sourceValues += $"'{sourceProjects[i].SourceId}',";
        iRetrieveMultipleCounter++;

        if (iRetrieveMultipleCounter == 50 || i == sourceProjects.Count - 1) {
          // remove trailing comma
          sourceValues = sourceValues.TrimEnd(',');
          // create the Url for the webapi call to get records.
          string webApiUrl = svc.BaseAddress + existingProjectsWebApi.Replace("{SOUREIDVALUES}", sourceValues);

          // Testing
          var formattedValueHeaders = new Dictionary<string, List<string>> {
                        { "Prefer", new List<string>
                            { "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"" }
                        }
                    };
          JToken existingProjects = svc.Get(webApiUrl, formattedValueHeaders);

          JToken testPrjCollection = existingProjects["value"];
          
          foreach (JObject prj in testPrjCollection) {
            _existingProjects.Add(new ExistingTargetProject(prj));
          }

          iRetrieveMultipleCounter = 0;
        }
      }
      return _existingProjects;
    }

    public class ExistingTargetProject {
      private List<ExistingTargetProjectTask> _projectTasks;
      private List<ExistingTargetProjectTeamMember> _projectTeamMembers;
      
      private ExistingTargetProject() { } // hide default constructor
      public ExistingTargetProject(JObject existingProject) {
        Name = $"{existingProject["msdyn_subject"]}";
        Customer = existingProject.SelectToken("msdyn_customer", false);
        Description = $"{existingProject.SelectToken("msdyn_description", false)}";
        CalendarTemplate = existingProject.SelectToken("msdyn_workhourtemplate", false);

        foreach (JObject prjTask in existingProject["msdyn_msdyn_project_msdyn_projecttask_project"]) {
          ProjectTasks.Add(new ExistingTargetProjectTask(prjTask));
        }

      }

      public List<ExistingTargetProjectTask> ProjectTasks {
        get {
          if (_projectTasks == null)
            _projectTasks = new List<ExistingTargetProjectTask>();
          return _projectTasks;
        }
      }

      public List<ExistingTargetProjectTeamMember> ProjectTeamMembers {
        get {
          if (_projectTeamMembers == null)
            _projectTeamMembers = new List<ExistingTargetProjectTeamMember>();
          return _projectTeamMembers;
        }
      }

      public string Name { get; set; }
      public string Description { get; set; }
      public JToken Customer { get; set; }
      public JToken CalendarTemplate { get; set; }
      public string SourceID { get; set; }

    }

    public class ExistingTargetProjectTask {
      private ExistingTargetProjectTask() { } // hide default construction
      public ExistingTargetProjectTask(JToken prjTask) {
       // Category = prjTask.SelectToken("_msdyn_resourcecategorypricingdimension_value", false).ToString().Equals("null") ? null : new Guid(prjTask.SelectToken("_msdyn_resourcecategorypricingdimension_value", false).ToString());
        //ProjectBucket = prjTask.SelectToken("msdyn_projectbucket", false);
        //Role = prjTask.SelectToken("msdyn_resourcecategorypricingdimension", false);
        //OrganizationalUnit = prjTask.SelectToken("msdyn_organizationalunitpricingdimension", false);
        ScheduledStartDate = DateTime.Parse(prjTask.SelectToken("msdyn_scheduledstart", true).ToString());
        ScheduledEndDate = DateTime.Parse(prjTask.SelectToken("msdyn_scheduledend", true).ToString());
        //ParentTask = prjTask.SelectToken("msdyn_parenttask", false);
        Effort = Decimal.Parse(prjTask.SelectToken("msdyn_effort", true).ToString());
        TaskName = prjTask.SelectToken("msdyn_subject", true).ToString();
      }

      Guid? Category { get; set; }
      Guid? ProjectBucket { get; set; }
      Guid? Role { get; set; }
      Guid? OrganizationalUnit { get; set; }
      DateTime ScheduledStartDate { get; set; }
      DateTime ScheduledEndDate { get; set; }
      Guid? ParentTask { get; set; }
      Decimal Effort { get; set; }
      String TaskName { get; set; }
    }

    public class ExistingTargetProjectTeamMember {

    }

    public class ExistingTargetResourceAssignment {

    }

    public class ExistingTargetTaskDependancy {

    }

    public class SourceProject {
      private List<SourceProjectTask> _projectTasks = new List<SourceProjectTask>();
      private List<SourceProjectTeamMember> _projectTeamMembers = new List<SourceProjectTeamMember>();
      
      private SourceProject() { } // hide default constructor
      public SourceProject(string projectsourceid, int numberOfTasks, int numberOfTeamMembers, bool useParentTasks, bool useDependency) {
        SourceId = projectsourceid;
        Name = $"{projectsourceid}";

        // Add project team members
        int iBookableResourceIndex = 0;
        for (int i = 1; i <= numberOfTeamMembers; i++) {
          ProjecTeamMembers.Add(new SourceProjectTeamMember {
            ResourceName = ScheduleAPIDataMigrationHelpers.BookableResourceNames[iBookableResourceIndex]
          });
          iBookableResourceIndex = iBookableResourceIndex == 9 ? 0 : ++iBookableResourceIndex;
        }

        // Add project tasks
        int iChildTasksCreated = 0;
        iBookableResourceIndex = 0;
        int iParentTaskIndex = 1;
        SourceProjectTask parentTask = null;
        for (int i = 1; i <= numberOfTasks; i++) {
          // allow 10 child tasks per project.
          // Assumption is a max of 100 lowest leval projects tasks
          // during this sample.  This would add 10 parent tasks.
          iChildTasksCreated = iChildTasksCreated == 9 ? 0 : ++iChildTasksCreated;
          // if we are using parent tasks then we will create 10 child tasks per parent task.
          if (useParentTasks && iChildTasksCreated == 0) {
            // setup a new parent task to create the next 10 child tasks for.
            parentTask = new SourceProjectTask() {
              SourceId = $"PARENTTASK-{iParentTaskIndex} - {projectsourceid}",
              Name = $"PARENTTASK {iParentTaskIndex} - {projectsourceid}",
              Description = $"Parent task {iParentTaskIndex} - for project {projectsourceid}"
            };
            ProjectTasks.Add(parentTask);
          }

          // Add the low level task.
          // all low level tasks where assignments and dependencies will be applied.
          SourceProjectTask newSourceProjectTask = new SourceProjectTask() {
            Effort = 40, // effort in hours, this sample will create fixed effort scheduled projects
            SourceId = $"SRCTASK-{iChildTasksCreated} - {projectsourceid}",
            Description = $"Sample task for project {projectsourceid}, task sourceid SRCTASK-{iChildTasksCreated} - {projectsourceid}",
            ParentTask = parentTask // will be null if we are not using parent tasks.
          };

          // Add assignments to the source project task.
          newSourceProjectTask.TaskAssignments.Add(ProjecTeamMembers[iBookableResourceIndex]);
          iBookableResourceIndex++;
          newSourceProjectTask.TaskAssignments.Add(ProjecTeamMembers[iBookableResourceIndex]);
          iBookableResourceIndex = iBookableResourceIndex == 9 ? 0 : ++iBookableResourceIndex;

          ProjectTasks.Add(newSourceProjectTask);

          // TODO: Add logic to setup the dependencies.

        }
      }
      public string SourceId { get; set; }
      public string Name { get; set; }
      public DateTime EstimatedStartDate { get; set; }

      public List<SourceProjectTask> ProjectTasks {
        get { return _projectTasks; }
      }

      public List<SourceProjectTeamMember> ProjecTeamMembers {
        get { return _projectTeamMembers; }
      }
    }

    public class SourceProjectTask {
      private List<SourceProjectTeamMember> _projectTeamMembers = new List<SourceProjectTeamMember>();
      public string SourceId { get; set; }
      public string Name { get; set; }
      public string Description { get; set; }
      public SourceProjectTask ParentTask { get; set; }
      public DateTime StartDate { get; set; }      
      public DateTime EndDate { get; set; }
      public int Duration { get; set; }
      public int Effort { get; set; }

      public List<SourceProjectTeamMember> TaskAssignments {
        get {
          return _projectTeamMembers;
        }
      }
    }

    public class SourceProjectTeamMember {      
      public string ResourceName { get; set; }

      // TODO: Add proporties reqiured for ProjOps Migration
    }

    public class SourceProjectDependency {
      public SourceProjectTask DependsOnTask { get; set; }

      // TODO: Add properties required for ProjOps Migration
    }
  }
}
