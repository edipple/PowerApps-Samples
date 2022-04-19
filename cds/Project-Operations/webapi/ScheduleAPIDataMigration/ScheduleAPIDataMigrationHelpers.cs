using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerApps.Samples {
  public class ScheduleAPIDataMigrationHelpers {
    
    public static string[] BookableResourceNames = {"Abraham McCormick", "Allison Dickson", "Ashley Chinn", "Bernadette Foley", "Bob Kozak", "Brady Hannon", "Cheri Castaneda", "Christal Robles", "Christie Dawson", "Clarence Desimone" };
    
    // Load a simulated collection of source projects to process.
    public static List<SourceProject> GetSourceProjects(int projectsToProcess, int numberOfTasks, int numberOfTeamMembers, bool useParentTasks, bool useDependency) {
      List<SourceProject> _sourceProjects = new List<SourceProject>();

      for (int i = 0; i < projectsToProcess; i++) {
        _sourceProjects.Add(new SourceProject($"SRCPRJ-{i}", numberOfTasks, numberOfTeamMembers, useParentTasks, useDependency));
      }

      return _sourceProjects;
    }

    public static ConcurrentBag<ExistingProject> GetExistingProjects(CDSWebApiService svc, List<SourceProject> sourceProjects) {
      ConcurrentBag<ExistingProject> _existingProjects = new ConcurrentBag<ExistingProject>();

      // TODO: This is where we will implement the parallel processing to load existing projects.
      return _existingProjects;
    }

    public class ExistingProject {
      private ConcurrentBag<JObject> _projectTasks = new ConcurrentBag<JObject>();
      
      private ExistingProject() { }
      public ExistingProject(CDSWebApiService svc, JObject existingProject) {
        // TODO Add code to populate the properties

        // Load the existing project tasks for this project.
        LoadExistingProjectTasks(svc);
      }

      public string Name { get; set; }

      public ConcurrentBag<JObject> ProjectTasks {
        get { return _projectTasks; }        
      }

      private void LoadExistingProjectTasks(CDSWebApiService svc) {
        // TODO Add 
      }
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
