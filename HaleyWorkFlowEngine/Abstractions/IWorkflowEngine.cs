using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Abstractions {
    public interface IWorkflowEngine {
        Task<IFeedback<Guid>> StartWorkflow(Guid definitionId, WorkflowPayload? payload);
        Task<IFeedback<Guid>> StartWorkflow(int code, int source, WorkflowPayload? payload);
        Task ExecuteAsync(Guid instanceId);
        //Task<WorkflowDefinition> LoadDefinitionByGuidAsync(Guid def_guid);
        //Task<WorkflowDefinition> LoadDefinitionByWFCode(int wf_code, int source = 0);
        //Task<StepResult> ExecuteStepAsync(WorkflowStep step, Dictionary<string, object> parameters, Dictionary<string, string> urlOverrides);
        //Task MonitorTimeoutAsync(WorkflowStep step, WorkflowInstance instance, WorkflowState state);
        Task HandleWebhookAsync(Guid instanceId, string eventKey, Dictionary<string, object> payload);
    }
}
