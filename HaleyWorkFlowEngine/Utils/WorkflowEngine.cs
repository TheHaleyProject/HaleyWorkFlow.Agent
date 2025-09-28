using Haley.Enums;
using Haley.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Abstractions;

namespace Haley.Utils {
    public class WorkflowEngine : IWorkflowEngine {
        readonly string Id = Guid.NewGuid().ToString(); //This is my engine Id.
        private readonly IWorkflowRepository _repository;
        private readonly ILogger<WorkflowEngine> _logger;
        private readonly Dictionary<Guid, WorkflowInstance> _activeInstances = new();
        private readonly Dictionary<Guid, WorkflowDefinition> _definitionCache = new(); //Get the definition only based on the GUID.

        public WorkflowEngine(IWorkflowRepository repository, ILogger<WorkflowEngine> logger) {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IFeedback<Guid>> StartWorkflow(int code, int source, WorkflowPayload? payload) {
            var definition = await LoadDefinitionAsync(code,source);
            if (payload == null) payload = new WorkflowPayload();
            payload.Definition = definition;
            return await StartWorkFlowInternal(payload);
        }

        public async Task<IFeedback<Guid>> StartWorkflow(Guid definitionId, WorkflowPayload? payload) {
            var definition = await LoadDefinitionAsync(definitionId);
            if (payload == null) payload = new WorkflowPayload();
            payload.Definition = definition;
            return await StartWorkFlowInternal(payload);
        }

        async Task<IFeedback<Guid>> StartWorkFlowInternal(WorkflowPayload payload) {
            if (payload.Definition == null) throw new ArgumentNullException($@"{nameof(WorkflowPayload)} doesn't contain a valid {nameof(WorkflowDefinition)}");

            var instance = new WorkflowInstance {
                InstanceId = Guid.NewGuid(), //Are we sure that we are creating this instance here & just synchronizing in the database?
                DefinitionId = payload.Definition.Guid,
                Parameters = payload.Parameters,
                Urls = payload.Urls,
                State = WorkflowStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                Environment = payload.Environment,
                Name = payload.Definition.Name,
                Owner = payload.Owner,
                Reference = payload.Reference
            };

            _activeInstances[instance.InstanceId] = instance;
            await _repository.SaveInstanceAsync(instance);
            _logger.LogInformation($"Workflow {instance.InstanceId} started.");

            return new Feedback<Guid>(true) { Result = instance.InstanceId };
        }

        public async Task ExecuteAsync(Guid instanceId) {
            if (!_activeInstances.TryGetValue(instanceId, out var instance)) {
                instance = await _repository.LoadInstanceAsync(instanceId);
                if (instance == null) throw new InvalidOperationException("Workflow instance not found.");
                _activeInstances[instanceId] = instance;
            }

            var definition = await LoadDefinitionAsync(instance.DefinitionId);
            var state = new WorkflowState {
                Status = WorkflowStatus.Running,
                StepResults = new Dictionary<int, StepResult>(),
                RuntimeContext = new Dictionary<string, object>(),
                Logs = new List<StepLog>()
            };

            foreach (var phase in definition.Phases) {
                state.CurrentPhaseCode = phase.Code;
                foreach (var stepCode in phase.Steps) {
                    var step = definition.Steps.First(s => s.Code == stepCode);
                    state.CurrentStepCode = step.Code;

                    var result = await ExecuteStepAsync(step, instance.Parameters, instance.Urls);
                    state.StepResults[step.Code] = result;
                    state.Logs.Add(new StepLog {
                        StepCode = step.Code,
                        Status = result.Status,
                        Timestamp = DateTime.UtcNow,
                        Message = result.ErrorMessage ?? "Step executed."
                    });

                    if (result.Status == WorkflowStatus.Failed) {
                        state.Status = WorkflowStatus.Failed;
                        break;
                    }
                }

                if (state.Status == WorkflowStatus.Failed)
                    break;
            }

            instance.State = state.Status;
            instance.LastUpdated = DateTime.UtcNow;
            //await _repository.UpdateInstanceAsync(instance, state);
            _logger.LogInformation($"Workflow {instanceId} completed with status {state.Status}.");
        }

        async Task<WorkflowDefinition> LoadDefinitionAsync(int wf_code, int source = 0) {
            var guidObj = await _repository.GetGuidByWfCode(wf_code,source); //Always fetch the latest item.
            if (!guidObj.Status) throw new Exception(guidObj.Message);
            return await LoadDefinitionAsync(guidObj.Result);
        }

        async Task<WorkflowDefinition> LoadDefinitionAsync(Guid def_guid) {
            if (_definitionCache.TryGetValue(def_guid, out var cached))
                return cached;

            var defObj = await _repository.LoadWorkflow(def_guid);
            if (!defObj.Status || defObj.Result == null) throw new Exception(defObj.Message);
            _definitionCache[def_guid] = defObj.Result;
            return defObj.Result;
        }

        private async Task<StepResult> ExecuteStepAsync(WorkflowStep step, Dictionary<string, object> parameters, Dictionary<string, string> urlOverrides) {
            // Simulate execution logic
            await Task.Delay(100); // Replace with actual dispatch logic

            return new StepResult {
                Status = WorkflowStatus.Completed,
                Output = new { success = true },
                StartedAt = DateTime.UtcNow.AddSeconds(-1),
                CompletedAt = DateTime.UtcNow
            };
        }

        private async Task MonitorTimeoutAsync(WorkflowStep step, WorkflowInstance instance, WorkflowState state) {
            if (TimeSpan.TryParse(step.Timeout, out var timeout)) {
                var deadline = state.StepResults[step.Code].StartedAt?.Add(timeout);
                if (deadline.HasValue && DateTime.UtcNow > deadline.Value) {
                    state.StepResults[step.Code].Status = WorkflowStatus.TimeOut;
                    state.Logs.Add(new StepLog {
                        StepCode = step.Code,
                        Status = WorkflowStatus.TimeOut,
                        Timestamp = DateTime.UtcNow,
                        Message = $"Step {step.Code} timed out after {timeout}"
                    });

                    if (step.OnTimeout?.Steps != null) {
                        //foreach (var fallbackCode in step.OnTimeout.Steps) {
                        //    var fallbackStep = instance.Definition.Steps.First(s => s.Code == fallbackCode);
                        //    await ExecuteStepAsync(fallbackStep, instance.Parameters, instance.UrlOverrides);
                        //}
                    }
                }
            }
        }

        public async Task HandleWebhookAsync(Guid instanceId, string eventKey, Dictionary<string, object> payload) {
            //    var instance = await _repository.LoadInstanceAsync(instanceId);
            //    var state = await _repository.LoadStateAsync(instanceId);

            //    // Match event to step trigger
            //    var triggeredStep = instance.Definition.Steps.FirstOrDefault(s => s.Trigger == eventKey);
            //    if (triggeredStep != null) {
            //        var result = await ExecuteStepAsync(triggeredStep, payload, instance.UrlOverrides);
            //        state.StepResults[triggeredStep.Code] = result;
            //        state.Status = result.Status;
            //        await _repository.UpdateInstanceAsync(instance, state);
            //    }
        }
    }
}
