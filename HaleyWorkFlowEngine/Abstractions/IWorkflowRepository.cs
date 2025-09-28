using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Abstractions {
    public interface IWorkflowRepository {
        // --- App Source --
        Task<IFeedback<Dictionary<string, object>>> CreateOrGetAppSourceAsync(int code, string name);

        // --- Workflow (root entity) ---
        Task<WorkflowDefinition> LoadWorkflowAsync(int id);
        Task<IEnumerable<WorkflowDefinition>> LoadAllWorkflowsAsync();
        Task<IFeedback<WorkflowDefinition>> LoadWorkflow(int code, int source);
        Task<IFeedback<WorkflowDefinition>> LoadWorkflow(Guid guid);
        Task<IFeedback<Dictionary<string, object>>> CreateOrGetWorkflow(int code, string name, int source = 0);
        Task<IFeedback<Guid>> GetGuidByWfCode(int code, int source = 0);
        Task UpdateWorkflow(int code, string name, int source = 0);
        Task DeleteWorkflowAsync(int code);

        // --- Workflow Versions ---
        Task<WorkflowDefinition> LoadVersionAsync(Guid versionGuid);
        Task<WorkflowDefinition> LoadLatestVersionAsync(int workflowId);
        Task<IFeedback<Guid>> CreateVersionAsync(int workflowId, string definitionJson);
        Task<IFeedback> GetWFVersion(int workflowId);
        Task<IFeedback> GetWFVersion(Guid guid);
        Task<IFeedback> GetWFVersion(int wf_code, int source);
        Task MarkVersionAsPublishedAsync(Guid versionGuid);
        Task<int> GetVersionIdByDefinitionGuidAsync(Guid versionGuid);

        // --- Workflow Instances ---
        Task SaveInstanceAsync(WorkflowInstance instance);
        Task<WorkflowInstance> LoadInstanceAsync(Guid instanceGuid);
        Task UpdateInstanceAsync(WorkflowInstance instance);

        // --- Workflow State ---
        Task<WorkflowState> LoadStateAsync(Guid instanceGuid);
        Task UpdateStateAsync(Guid instanceGuid, WorkflowState state);

        // --- Logs / Steps (optional, can be split into separate repos) ---
        Task<IEnumerable<WorkflowStep>> LoadStepsAsync(Guid instanceGuid);
        Task<IEnumerable<StepLog>> LoadLogsAsync(Guid instanceGuid);
        Task AddStepLogAsync(StepLog log);
    }
}
