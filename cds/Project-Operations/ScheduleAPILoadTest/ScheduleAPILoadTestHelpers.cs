using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace PowerApps.Samples {
  public class ScheduleAPILoadTestHelpers {
    public static List<Entity> RetrieveAllRecords(CrmServiceClient svc, QueryExpression qe) {
      List<Entity> col = new List<Microsoft.Xrm.Sdk.Entity>();

      qe.PageInfo = new PagingInfo() {
        Count = 5000,
        PageNumber = 1
      };

      // get the first page of results.
      EntityCollection colPage = null;
      colPage = svc.RetrieveMultiple(qe);

      if (colPage == null || colPage.Entities == null || colPage.Entities.Count == 0)
        return col;

      foreach (Entity ent in colPage.Entities) {
        col.Add(ent);
      }

      if (!colPage.MoreRecords)
        return col; // no more records just return what we have.

      while (colPage.MoreRecords) {
        qe.PageInfo.PagingCookie = colPage.PagingCookie;
        qe.PageInfo.PageNumber++;
        colPage.Entities.Clear();
        colPage = null;
        colPage = svc.RetrieveMultiple(qe);
        foreach (Entity ent in colPage.Entities) {
          col.Add(ent);
        }
      }
      return col;
    }

    public static List<Entity> RetrieveAllRecords(CrmServiceClient svc, string fetchXml) {
      List<Entity> col = new List<Microsoft.Xrm.Sdk.Entity>();
      return RetrieveAllRecords(svc, ConvertToQueryExpression(svc, fetchXml));
    }

    public static QueryExpression ConvertToQueryExpression(CrmServiceClient svc, string fetchXml) {
      FetchExpression fetchClosedBookings = new FetchExpression(fetchXml);
      var conversionRequest = new FetchXmlToQueryExpressionRequest {
        FetchXml = fetchClosedBookings.Query
      };
      var conversionResponse = (FetchXmlToQueryExpressionResponse)svc.Execute(conversionRequest);
      QueryExpression qe = conversionResponse.Query;
      return qe;
    }

    // Simulate a list of source projects to migrate.
    public static List<SourceProject> GetSourceProjects(int iProjectsToMigrate) {
      List<SourceProject> sourceProjects = new List<SourceProject>();
      for (int i = 1; i <= iProjectsToMigrate; i++) {
        sourceProjects.Add(new SourceProject($"SRCPRJ-{i}") {
          Description = $"Description for project SRCPRJ-{i}",
          ProjectName = $"PRJ - {i}"
        });
      }
      return sourceProjects;
    }

    // Yep a very ineficient way of loading the existing data but that is not the focus of this sample.
    // So just do a simple hack to load the existing projects.
    public static List<ExistingProject> LoadExistingProjects(CrmServiceClient svc, List<SourceProject> srcProjects) {
      const string fetchXmlExistingProjects = @"
<fetch no-lock='true'>
  <entity name='msdyn_project'>
    entity name='msdyn_project'>
    <attribute name='msdyn_subject' />
    <attribute name='msftce_sourceid' />
    <attribute name='msdyn_projectid' />
    <filter>
      <condition attribute='msftce_sourceid' operator='in'>
        {SOURCELIST}
      </condition>
    </filter>
  </entity>
</fetch>";
      /*<value>SRCPRJ-1</value>
          <value>SRCPRJ-2</value>
          <value>SRCPRJ-3</value>*/

      // build fetchxml queries for batchs of 20 projects
      List<ExistingProject> existingProjects = new List<ExistingProject>();
      List<string> batchFetchXml = new List<string>();
      string fetchXmlValues = "";
      for (int i = 0; i < srcProjects.Count; i++) {
        fetchXmlValues += $"<value>{srcProjects[i].SourceId}</value>";
        if (i > 0 && i % 20 == 0) {
          batchFetchXml.Add(fetchXmlExistingProjects.Replace("{SOURCELIST}", fetchXmlValues));
          fetchXmlValues = "";
        }
      }
      // add the last query
      batchFetchXml.Add(fetchXmlExistingProjects.Replace("{SOURCELIST}", fetchXmlValues));

      // Load the projects for each batch.  Yea could do this using parallel threads but not the focus of this sample.
      foreach (string fetchXml in batchFetchXml) {
        foreach (Entity e in ScheduleAPILoadTestHelpers.RetrieveAllRecords(svc, fetchXml)) {
          existingProjects.Add(new ExistingProject(svc, e));
        }
      }
      return existingProjects;
    }

    [DataContract]
    public class OperationSetResponse {
      [DataMember(Name = "operationSetId")]
      public Guid OperationSetId { get; set; }

      [DataMember(Name = "operationSetDetailId")]
      public Guid OperationSetDetailId { get; set; }

      [DataMember(Name = "operationType")]
      public string OperationType { get; set; }

      [DataMember(Name = "recordId")]
      public string RecordId { get; set; }

      [DataMember(Name = "correlationId")]
      public string CorrelationId { get; set; }
    }

    [DataContract]
    public class ExistingProject {
      private List<Entity> _existingProjectTasks = null;

      // Hide default constructor
      private ExistingProject() { }

      public ExistingProject(CrmServiceClient svc, Entity existingProject) {
        // Yes this is extremely ineficient but a simple way to get existing project information.
        // The purpose of this sample is NOT how to get source projects or existing project information rather
        // how to maximize use of the ScheduleAPIs for data migration.  As such I am just doing a simple
        // hack to load the existing project information.

        ProjectId = existingProject.Id;
        ProjectEntity = existingProject;
        SourceId = existingProject.GetAttributeValue<string>("msftce_sourceid"); // Assume that all existing projects have this attribute set.
        Name = existingProject.GetAttributeValue<string>("msdyn_subject");

        // Get the existing tasks for this project.
        LoadExistingProjectTasks(svc);

        // Get the default project bucket.  Again the assumption is that all projects will have the default bucket
        // which is auto created when you create a new project.  If your data migration needs to put project tasks
        // into buckets that were part of your source environment you will need to add logic to handle this.
        LoadDefaultProjectBucket(svc);
      }

      public void LoadDefaultProjectBucket(CrmServiceClient svc) {
        string fetchXml = @"
<fetch no-lock='true'>
  <entity name='msdyn_projectbucket'>
    <attribute name='msdyn_projectbucketid' />
    <attribute name='msdyn_project' />
    <attribute name='msdyn_name' />
    <filter>
      <condition attribute='msdyn_project' operator='eq' value='{PROJECTID}' />
      <condition attribute='msdyn_name' operator='eq' value='Bucket 1' />    
    </filter>
  </entity>
</fetch>";
        List<Entity> existingBucket = ScheduleAPILoadTestHelpers.RetrieveAllRecords(svc, fetchXml.Replace("{PROJECTID}", ProjectId.ToString()));
        if (existingBucket != null && existingBucket.Count > 0)
          ProjectBucket = existingBucket[0];
      }

      public void LoadExistingProjectTasks(CrmServiceClient svc) {
        string fetchXml = @"
<fetch no-lock='true'>
  <entity name='msdyn_projecttask'>
    <attribute name='msdyn_subject' />
    <attribute name='msdyn_projecttaskid' />
    <attribute name='msftce_sourceid' />
    <filter>
      <condition attribute='msdyn_project' operator='eq' value='{PROJECTID}' />
    </filter>
  </entity>
</fetch>";

        List<Entity> existingTasks = ScheduleAPILoadTestHelpers.RetrieveAllRecords(svc, fetchXml.Replace("{PROJECTID}", ProjectId.ToString()));
        foreach (Entity existingTask in existingTasks) {
          ProjectTasks.Add(existingTask);
        }
      }

      public Entity HasExistingProjectTask(string srcId) {
        Entity entExistingTask = null;
        foreach (Entity task in ProjectTasks) {
          if (task.GetAttributeValue<string>("msftce_sourceid").Equals(srcId)) {
            entExistingTask = task;
            break;
          }
        }
        return entExistingTask;
      }
      
      [DataMember(Name = "projectid")]
      public Guid ProjectId { get; set; }

      [DataMember(Name = "sourceid")]
      public string SourceId { get; set; }

      [DataMember(Name = "name")]
      public string Name { get; set; }

      [DataMember(Name = "projectentity")]
      public Entity ProjectEntity { get; set; }

      [DataMember(Name = "projectbucket")]
      public Entity ProjectBucket { get; set; }

      [DataMember(Name = "projecttasks")]
      public List<Entity> ProjectTasks {
        get {
          if (_existingProjectTasks == null)
            _existingProjectTasks = new List<Entity>();
          return _existingProjectTasks;
        }
      }
    }

    [DataContract]
    public class SourceProject {
      private List<SourceProjectTask> _projectTasks = null;

      public SourceProject(string srcid) {
        SourceId = srcid;
      }

      [DataMember(Name = "sourceid")]
      public string SourceId { get; set; }

      [DataMember(Name = "projectname")]
      public string ProjectName { get; set; }

      [DataMember(Name = "description")]
      public string Description { get; set; }

      [DataMember(Name = "projecttasks")]
      public List<SourceProjectTask> ProjectTasks {
        get {
          if (_projectTasks == null) {
            _projectTasks = new List<SourceProjectTask>();
            // create 300 project tasks per project.
            for (int iTaskCounter = 1; iTaskCounter <= 300; iTaskCounter++) {
              _projectTasks.Add(new SourceProjectTask {
                Description = $"Description for project task: {SourceId} - Project Task #{iTaskCounter}",
                SourceId = $"SRCPRJTSK-{iTaskCounter}-{SourceId}",
                SourceProjectId = SourceId,
                TaskName = $"{SourceId} - Project Task #{iTaskCounter}"
              });
            }
          }
          return _projectTasks;
        }
      }
    }

    [DataContract]
    public class SourceProjectTask {
      [DataMember(Name = "sourceid")]
      public string SourceId { get; set; }

      [DataMember(Name = "taskname")]
      public string TaskName { get; set; }

      [DataMember(Name = "description")]
      public string Description { get; set; }

      [DataMember(Name = "sourceprojectid")]
      public string SourceProjectId { get; set; }
    }
  }
}
