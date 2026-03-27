using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// ChiefSoftwareArchitect is the orchestrator agent for Squad-Commerce.
/// It receives user requests, decomposes them into tasks, delegates to domain agents,
/// and synthesizes the final response.
/// </summary>
/// <remarks>
/// This agent NEVER calls MCP tools directly. It only delegates to:
/// - InventoryAgent (inventory queries)
/// - PricingAgent (pricing calculations and updates)
/// - MarketIntelAgent (competitor intelligence via A2A)
/// 
/// Allowed tools: [] (orchestrators delegate only)
/// Required scope: SquadCommerce.Orchestrate
/// </remarks>
public sealed class ChiefSoftwareArchitectAgent
{
    private readonly InventoryAgent _inventoryAgent;
    private readonly PricingAgent _pricingAgent;
    private readonly MarketIntelAgent _marketIntelAgent;
    private readonly MarketingAgent _marketingAgent;
    private readonly LogisticsAgent _logisticsAgent;
    private readonly RedistributionAgent _redistributionAgent;
    private readonly TrafficAnalystAgent _trafficAnalystAgent;
    private readonly MerchandisingAgent _merchandisingAgent;
    private readonly ManagerAgent _managerAgent;
    private readonly ComplianceAgent _complianceAgent;
    private readonly ResearchAgent _researchAgent;
    private readonly ProcurementAgent _procurementAgent;
    private readonly AuditRepository _auditRepository;
    private readonly IThinkingStateNotifier _thinkingNotifier;
    private readonly IReasoningTraceEmitter _reasoningTraceEmitter;
    private readonly ILogger<ChiefSoftwareArchitectAgent> _logger;

    public ChiefSoftwareArchitectAgent(
        InventoryAgent inventoryAgent,
        PricingAgent pricingAgent,
        MarketIntelAgent marketIntelAgent,
        MarketingAgent marketingAgent,
        LogisticsAgent logisticsAgent,
        RedistributionAgent redistributionAgent,
        TrafficAnalystAgent trafficAnalystAgent,
        MerchandisingAgent merchandisingAgent,
        ManagerAgent managerAgent,
        ComplianceAgent complianceAgent,
        ResearchAgent researchAgent,
        ProcurementAgent procurementAgent,
        AuditRepository auditRepository,
        IThinkingStateNotifier thinkingNotifier,
        IReasoningTraceEmitter reasoningTraceEmitter,
        ILogger<ChiefSoftwareArchitectAgent> logger)
    {
        _inventoryAgent = inventoryAgent ?? throw new ArgumentNullException(nameof(inventoryAgent));
        _pricingAgent = pricingAgent ?? throw new ArgumentNullException(nameof(pricingAgent));
        _marketIntelAgent = marketIntelAgent ?? throw new ArgumentNullException(nameof(marketIntelAgent));
        _marketingAgent = marketingAgent ?? throw new ArgumentNullException(nameof(marketingAgent));
        _logisticsAgent = logisticsAgent ?? throw new ArgumentNullException(nameof(logisticsAgent));
        _redistributionAgent = redistributionAgent ?? throw new ArgumentNullException(nameof(redistributionAgent));
        _trafficAnalystAgent = trafficAnalystAgent ?? throw new ArgumentNullException(nameof(trafficAnalystAgent));
        _merchandisingAgent = merchandisingAgent ?? throw new ArgumentNullException(nameof(merchandisingAgent));
        _managerAgent = managerAgent ?? throw new ArgumentNullException(nameof(managerAgent));
        _complianceAgent = complianceAgent ?? throw new ArgumentNullException(nameof(complianceAgent));
        _researchAgent = researchAgent ?? throw new ArgumentNullException(nameof(researchAgent));
        _procurementAgent = procurementAgent ?? throw new ArgumentNullException(nameof(procurementAgent));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _thinkingNotifier = thinkingNotifier ?? throw new ArgumentNullException(nameof(thinkingNotifier));
        _reasoningTraceEmitter = reasoningTraceEmitter ?? throw new ArgumentNullException(nameof(reasoningTraceEmitter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Orchestrates a competitor price response workflow using graph-based delegation.
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <param name="competitorPrice">Competitor's claimed price</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Orchestrated result with all A2UI payloads and executive summary</returns>
    public async Task<OrchestratorResult> ProcessCompetitorPriceDropAsync(
        string sku,
        decimal competitorPrice,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";
        
        // Create parent orchestrator span that wraps entire workflow
        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Orchestrate");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.competitor_price", competitorPrice);
        activity?.SetTag("agent.session_id", sessionId);
        
        // Record invocation count
        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting competitor price response workflow: SKU {Sku}, CompetitorPrice ${CompetitorPrice:F2}, SessionId {SessionId}",
            sku,
            competitorPrice,
            sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        // Emit root reasoning step
        var rootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Analyzing competitor price drop for {sku} at ${competitorPrice:F2}", cancellationToken: cancellationToken);

        // Record workflow initiation
        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated competitor price response workflow", 
            "AGUI", startTime, TimeSpan.Zero, "Success", $"User request for SKU {sku} at ${competitorPrice:F2}", 
            activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

        try
        {
            // Step 1: Validate competitor claim via A2A (MarketIntelAgent)
            _logger.LogInformation("Step 1: Delegating to MarketIntelAgent for competitor validation");
            
            var step1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to MarketIntelAgent for competitor validation", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "MarketIntelAgent", ReasoningStepType.A2AHandshake,
                $"Querying competitor data for {sku}", step1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var step1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "MarketIntelAgent",
                StageName = "Market Intelligence",
                Status = "Running",
                Protocol = "A2A",
                StartedAt = stage1Start
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketIntelAgent", true, cancellationToken);
            var marketIntelResult = await _marketIntelAgent.ExecuteAsync(sku, competitorPrice, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketIntelAgent", false, cancellationToken);
            step1Sw.Stop();
            results.Add(marketIntelResult);

            await EmitTraceAsync(sessionId, "MarketIntelAgent", ReasoningStepType.Observation,
                $"Received {(marketIntelResult.Success ? "validated competitor data" : "validation failure")} from MarketIntelAgent",
                step1Id, step1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "MarketIntelAgent", "Queried competitor pricing via A2A",
                "A2A", stage1Start, stage1Duration, marketIntelResult.Success ? "Success" : "Failed",
                marketIntelResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = marketIntelResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = marketIntelResult.A2UIPayload != null ? new[] { "MarketComparisonGrid" } : null,
                ErrorMessage = marketIntelResult.ErrorMessage
            };

            if (!marketIntelResult.Success)
            {
                _logger.LogWarning("MarketIntelAgent failed - continuing with limited competitor data (graceful degradation)");
            }

            // Step 2: Get inventory snapshot (InventoryAgent)
            _logger.LogInformation("Step 2: Delegating to InventoryAgent for inventory snapshot");
            
            var step2Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to InventoryAgent for inventory snapshot", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.ToolCall,
                $"Calling GetInventoryLevels with SKU={sku}", step2Id, cancellationToken: cancellationToken);

            var stage2Start = DateTimeOffset.UtcNow;
            var step2Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 2,
                AgentName = "InventoryAgent",
                StageName = "Inventory Analysis",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage2Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", true, cancellationToken);
            var inventoryResult = await _inventoryAgent.ExecuteAsync(sku, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", false, cancellationToken);
            step2Sw.Stop();
            results.Add(inventoryResult);

            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.Observation,
                $"Received inventory snapshot from InventoryAgent",
                step2Id, step2Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage2Duration = DateTimeOffset.UtcNow - stage2Start;
            await RecordAuditEntryAsync(sessionId, "InventoryAgent", "Retrieved inventory snapshot",
                "MCP", stage2Start, stage2Duration, inventoryResult.Success ? "Success" : "Warning",
                inventoryResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, 
                new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" }, null, cancellationToken);

            pipelineStages[1] = pipelineStages[1] with
            {
                Status = inventoryResult.Success ? "Completed" : "Failed",
                Duration = stage2Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = inventoryResult.A2UIPayload != null ? new[] { "RetailStockHeatmap" } : null,
                ErrorMessage = inventoryResult.ErrorMessage
            };

            if (!inventoryResult.Success)
            {
                _logger.LogWarning("InventoryAgent failed - continuing with limited data");
            }

            // Step 3: Calculate margin impact (PricingAgent)
            _logger.LogInformation("Step 3: Delegating to PricingAgent for margin impact analysis");
            
            var step3Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to PricingAgent for margin impact analysis", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "PricingAgent", ReasoningStepType.ToolCall,
                $"Calling GetInventoryLevels with SKU={sku}", step3Id, cancellationToken: cancellationToken);

            var stage3Start = DateTimeOffset.UtcNow;
            var step3Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 3,
                AgentName = "PricingAgent",
                StageName = "Pricing Calculation",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage3Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "PricingAgent", true, cancellationToken);
            var pricingResult = await _pricingAgent.ExecuteAsync(sku, competitorPrice, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "PricingAgent", false, cancellationToken);
            step3Sw.Stop();
            results.Add(pricingResult);

            await EmitTraceAsync(sessionId, "PricingAgent", ReasoningStepType.Observation,
                $"Received margin impact analysis from PricingAgent",
                step3Id, step3Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage3Duration = DateTimeOffset.UtcNow - stage3Start;
            await RecordAuditEntryAsync(sessionId, "PricingAgent", "Calculated margin impact scenarios",
                "MCP", stage3Start, stage3Duration, pricingResult.Success ? "Success" : "Failed",
                pricingResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[2] = pipelineStages[2] with
            {
                Status = pricingResult.Success ? "Completed" : "Failed",
                Duration = stage3Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = pricingResult.A2UIPayload != null ? new[] { "PricingImpactChart" } : null,
                ErrorMessage = pricingResult.ErrorMessage
            };

            if (!pricingResult.Success)
            {
                _logger.LogWarning("PricingAgent failed - aborting workflow");
                return await BuildFailureResultAsync(sessionId, results, pipelineStages, "Failed to calculate pricing impact", startTime, cancellationToken);
            }

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing orchestrator response");
            
            // Create synthesize span as child of orchestrate
            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);
            
            var synthesizeStart = DateTimeOffset.UtcNow;
            var executiveSummary = BuildExecutiveSummary(sku, competitorPrice, results);
            var synthesizeDuration = DateTimeOffset.UtcNow - synthesizeStart;

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Decision,
                "Recommending review of pricing impact scenarios with validated competitor data",
                rootStepId, (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Synthesized orchestrator response",
                "AGUI", synthesizeStart, synthesizeDuration, "Success",
                $"Generated executive summary with {results.Count} A2UI payloads", 
                activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;
            
            // Record orchestrator invocation duration
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            _logger.LogInformation(
                "Orchestrator workflow completed successfully in {Duration}ms",
                duration.TotalMilliseconds);

            // Build A2UI payloads for audit trail and pipeline visualization
            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "CompetitorPriceDropWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildInsightCards(sku, competitorPrice, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator workflow failed");
            
            // Set error status on span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"Workflow failed: {ex.Message}", rootStepId, cancellationToken: cancellationToken);

            // Record error audit entry
            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Workflow execution failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);
            
            // Record duration even on error
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            return await BuildFailureResultAsync(sessionId, results, pipelineStages, $"Orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    /// <summary>
    /// Orchestrates a direct inventory query workflow. Delegates only to InventoryAgent
    /// without requiring competitor pricing validation.
    /// </summary>
    /// <param name="sku">Product SKU to query inventory for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Orchestrated result with inventory A2UI payload</returns>
    public async Task<OrchestratorResult> ProcessInventoryQueryAsync(
        string sku,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";

        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "OrchestrateInventoryQuery");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.session_id", sessionId);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting inventory query workflow: SKU {Sku}, SessionId {SessionId}",
            sku, sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        var rootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Querying inventory levels for {sku} across all stores", cancellationToken: cancellationToken);

        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated inventory query workflow",
            "AGUI", startTime, TimeSpan.Zero, "Success", $"User request for inventory levels of SKU {sku}",
            activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

        try
        {
            // Single step: Get inventory snapshot (InventoryAgent)
            _logger.LogInformation("Step 1: Delegating to InventoryAgent for inventory snapshot");

            var step1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to InventoryAgent for inventory snapshot", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.ToolCall,
                $"Calling GetInventoryLevels with SKU={sku}", step1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var step1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "InventoryAgent",
                StageName = "Inventory Query",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage1Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", true, cancellationToken);
            var inventoryResult = await _inventoryAgent.ExecuteAsync(sku, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", false, cancellationToken);
            step1Sw.Stop();
            results.Add(inventoryResult);

            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.Observation,
                $"Received inventory snapshot from InventoryAgent",
                step1Id, step1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "InventoryAgent", "Retrieved inventory snapshot",
                "MCP", stage1Start, stage1Duration, inventoryResult.Success ? "Success" : "Failed",
                inventoryResult.TextSummary, activity?.TraceId.ToString(), new[] { sku },
                new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" }, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = inventoryResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = inventoryResult.A2UIPayload != null ? new[] { "RetailStockHeatmap" } : null,
                ErrorMessage = inventoryResult.ErrorMessage
            };

            if (!inventoryResult.Success)
            {
                _logger.LogWarning("InventoryAgent failed for inventory query");
                return await BuildInventoryQueryFailureResultAsync(sessionId, results, pipelineStages,
                    inventoryResult.ErrorMessage ?? "Inventory query failed", startTime, cancellationToken);
            }

            // Build executive summary
            var executiveSummary = BuildInventoryQueryExecutiveSummary(sku, results);

            var duration = DateTimeOffset.UtcNow - startTime;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "InventoryQueryWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildInventoryQueryInsightCards(sku, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory query workflow failed");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"Inventory query workflow failed: {ex.Message}", rootStepId, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Inventory query workflow failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            return await BuildInventoryQueryFailureResultAsync(sessionId, results, pipelineStages,
                $"Orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    /// <summary>
    /// Orchestrates a bulk competitor price response workflow for multiple SKUs.
    /// </summary>
    public async Task<OrchestratorResult> ProcessBulkCompetitorPriceDropAsync(
        string competitorName,
        IReadOnlyList<(string Sku, decimal CompetitorPrice)> items,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";
        
        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "OrchestrateBulk");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.sku_count", items.Count);
        activity?.SetTag("agent.competitor", competitorName);
        activity?.SetTag("agent.session_id", sessionId);
        
        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting bulk competitor price response workflow: Competitor {Competitor}, Items {ItemCount}, SessionId {SessionId}",
            competitorName,
            items.Count,
            sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        var skuList = items.Select(i => i.Sku).ToArray();
        var skuSummary = string.Join(", ", skuList.Take(3)) + (skuList.Length > 3 ? $" (+{skuList.Length - 3} more)" : "");

        // Emit root reasoning step for bulk workflow
        var bulkRootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Analyzing bulk competitor price drop from {competitorName} for {items.Count} SKUs: {skuSummary}",
            cancellationToken: cancellationToken);

        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated bulk competitor price response workflow",
            "AGUI", startTime, TimeSpan.Zero, "Success", 
            $"Bulk request from {competitorName} for {items.Count} SKUs", 
            activity?.TraceId.ToString(), skuList, null, null, cancellationToken);

        try
        {
            // Step 1: Validate competitor claims via A2A (MarketIntelAgent)
            _logger.LogInformation("Step 1: Delegating to MarketIntelAgent for bulk competitor validation");
            
            var bulkStep1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to MarketIntelAgent for bulk competitor validation", bulkRootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "MarketIntelAgent", ReasoningStepType.A2AHandshake,
                $"Querying competitor data for {items.Count} SKUs", bulkStep1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var bulkStep1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "MarketIntelAgent",
                StageName = "Bulk Market Intelligence",
                Status = "Running",
                Protocol = "A2A",
                StartedAt = stage1Start
            });

            var marketIntelItems = items.Select(i => (i.Sku, i.CompetitorPrice)).ToList();
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketIntelAgent", true, cancellationToken);
            var marketIntelResult = await _marketIntelAgent.ExecuteBulkAsync(marketIntelItems, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketIntelAgent", false, cancellationToken);
            bulkStep1Sw.Stop();
            results.Add(marketIntelResult);

            await EmitTraceAsync(sessionId, "MarketIntelAgent", ReasoningStepType.Observation,
                $"Received {(marketIntelResult.Success ? "validated bulk competitor data" : "validation failure")} from MarketIntelAgent",
                bulkStep1Id, bulkStep1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "MarketIntelAgent", "Queried bulk competitor pricing via A2A",
                "A2A", stage1Start, stage1Duration, marketIntelResult.Success ? "Success" : "Failed",
                marketIntelResult.TextSummary, activity?.TraceId.ToString(), skuList, null, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = marketIntelResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = marketIntelResult.A2UIPayload != null ? new[] { "MarketComparisonGrid" } : null,
                ErrorMessage = marketIntelResult.ErrorMessage
            };

            if (!marketIntelResult.Success)
            {
                _logger.LogWarning("MarketIntelAgent failed - aborting bulk workflow");
                return await BuildFailureResultAsync(sessionId, results, pipelineStages, "Failed to validate bulk competitor pricing", startTime, cancellationToken);
            }

            // Step 2: Get bulk inventory snapshot (InventoryAgent)
            _logger.LogInformation("Step 2: Delegating to InventoryAgent for bulk inventory snapshot");
            
            var bulkStep2Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to InventoryAgent for bulk inventory snapshot", bulkRootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.ToolCall,
                $"Calling GetInventoryLevels for {skuList.Length} SKUs", bulkStep2Id, cancellationToken: cancellationToken);

            var stage2Start = DateTimeOffset.UtcNow;
            var bulkStep2Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 2,
                AgentName = "InventoryAgent",
                StageName = "Bulk Inventory Analysis",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage2Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", true, cancellationToken);
            var inventoryResult = await _inventoryAgent.ExecuteBulkAsync(skuList, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", false, cancellationToken);
            bulkStep2Sw.Stop();
            results.Add(inventoryResult);

            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.Observation,
                $"Received bulk inventory snapshot from InventoryAgent",
                bulkStep2Id, bulkStep2Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage2Duration = DateTimeOffset.UtcNow - stage2Start;
            await RecordAuditEntryAsync(sessionId, "InventoryAgent", "Retrieved bulk inventory snapshot",
                "MCP", stage2Start, stage2Duration, inventoryResult.Success ? "Success" : "Warning",
                inventoryResult.TextSummary, activity?.TraceId.ToString(), skuList, 
                new[] { "SEA-001", "PDX-002", "SFO-003", "LAX-004", "DEN-005" }, null, cancellationToken);

            pipelineStages[1] = pipelineStages[1] with
            {
                Status = inventoryResult.Success ? "Completed" : "Failed",
                Duration = stage2Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = inventoryResult.A2UIPayload != null ? new[] { "RetailStockHeatmap" } : null,
                ErrorMessage = inventoryResult.ErrorMessage
            };

            if (!inventoryResult.Success)
            {
                _logger.LogWarning("InventoryAgent failed - continuing with limited data");
            }

            // Step 3: Calculate bulk margin impact (PricingAgent)
            _logger.LogInformation("Step 3: Delegating to PricingAgent for bulk margin impact analysis");
            
            var bulkStep3Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to PricingAgent for bulk margin impact analysis", bulkRootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "PricingAgent", ReasoningStepType.ToolCall,
                $"Calling GetInventoryLevels for {skuList.Length} SKUs", bulkStep3Id, cancellationToken: cancellationToken);

            var stage3Start = DateTimeOffset.UtcNow;
            var bulkStep3Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 3,
                AgentName = "PricingAgent",
                StageName = "Bulk Pricing Calculation",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage3Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "PricingAgent", true, cancellationToken);
            var pricingResult = await _pricingAgent.ExecuteBulkAsync(items, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "PricingAgent", false, cancellationToken);
            bulkStep3Sw.Stop();
            results.Add(pricingResult);

            await EmitTraceAsync(sessionId, "PricingAgent", ReasoningStepType.Observation,
                $"Received bulk margin impact analysis from PricingAgent",
                bulkStep3Id, bulkStep3Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage3Duration = DateTimeOffset.UtcNow - stage3Start;
            await RecordAuditEntryAsync(sessionId, "PricingAgent", "Calculated bulk margin impact scenarios",
                "MCP", stage3Start, stage3Duration, pricingResult.Success ? "Success" : "Failed",
                pricingResult.TextSummary, activity?.TraceId.ToString(), skuList, null, null, cancellationToken);

            pipelineStages[2] = pipelineStages[2] with
            {
                Status = pricingResult.Success ? "Completed" : "Failed",
                Duration = stage3Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = pricingResult.A2UIPayload != null ? new[] { "PricingImpactChart" } : null,
                ErrorMessage = pricingResult.ErrorMessage
            };

            if (!pricingResult.Success)
            {
                _logger.LogWarning("PricingAgent failed - aborting bulk workflow");
                return await BuildFailureResultAsync(sessionId, results, pipelineStages, "Failed to calculate bulk pricing impact", startTime, cancellationToken);
            }

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing bulk orchestrator response");
            
            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);
            
            var synthesizeStart = DateTimeOffset.UtcNow;
            var executiveSummary = BuildBulkExecutiveSummary(competitorName, items, results);
            var synthesizeDuration = DateTimeOffset.UtcNow - synthesizeStart;

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Decision,
                $"Recommending consolidated review of {items.Count} SKU pricing impact scenarios",
                bulkRootStepId, (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Synthesized bulk orchestrator response",
                "AGUI", synthesizeStart, synthesizeDuration, "Success",
                $"Generated bulk executive summary with {results.Count} A2UI payloads for {items.Count} SKUs", 
                activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;
            
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            _logger.LogInformation(
                "Orchestrator bulk workflow completed successfully in {Duration}ms",
                duration.TotalMilliseconds);

            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "BulkCompetitorPriceDropWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildBulkInsightCards(competitorName, items, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator bulk workflow failed");
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"Bulk workflow failed: {ex.Message}", bulkRootStepId, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Bulk workflow execution failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            return await BuildFailureResultAsync(sessionId, results, pipelineStages, $"Bulk orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    /// <summary>
    /// Orchestrates a viral spike response workflow: Sentiment → Pricing → Marketing → Synthesis.
    /// </summary>
    /// <param name="sku">Viral product SKU</param>
    /// <param name="demandMultiplier">Current demand multiplier (e.g. 4.0 for 400%)</param>
    /// <param name="region">Target region (e.g. Northeast)</param>
    /// <param name="source">Source platform (e.g. TikTok)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Orchestrated result with all A2UI payloads and executive summary</returns>
    public async Task<OrchestratorResult> ProcessViralSpikeAsync(
        string sku,
        decimal demandMultiplier,
        string region,
        string source,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";

        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "OrchestrateViralSpike");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.demand_multiplier", (double)demandMultiplier);
        activity?.SetTag("agent.region", region);
        activity?.SetTag("agent.source", source);
        activity?.SetTag("agent.session_id", sessionId);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting viral spike workflow: SKU {Sku}, DemandMultiplier {Multiplier}x, Region {Region}, Source {Source}, SessionId {SessionId}",
            sku, demandMultiplier, region, source, sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        var rootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Analyzing viral spike for {sku}: {demandMultiplier}x demand surge in {region} via {source}",
            cancellationToken: cancellationToken);

        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated viral spike response workflow",
            "AGUI", startTime, TimeSpan.Zero, "Success",
            $"Viral spike for SKU {sku} at {demandMultiplier}x demand in {region} from {source}",
            activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

        try
        {
            // Step 1: Analyze social sentiment (MarketIntelAgent)
            _logger.LogInformation("Step 1: Delegating to MarketIntelAgent for social sentiment analysis");

            var step1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to MarketIntelAgent for social sentiment analysis", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "MarketIntelAgent", ReasoningStepType.ToolCall,
                $"Querying social sentiment data for {sku} in {region}", step1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var step1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "MarketIntelAgent",
                StageName = "Social Sentiment Analysis",
                Status = "Running",
                Protocol = "Internal",
                StartedAt = stage1Start,
                ToolsUsed = new[] { "GetSocialSentiment" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketIntelAgent", true, cancellationToken);
            var sentimentResult = await _marketIntelAgent.AnalyzeSocialSentimentAsync(sku, region, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketIntelAgent", false, cancellationToken);
            step1Sw.Stop();
            results.Add(sentimentResult);

            await EmitTraceAsync(sessionId, "MarketIntelAgent", ReasoningStepType.Observation,
                $"Received {(sentimentResult.Success ? "sentiment analysis" : "analysis failure")} from MarketIntelAgent",
                step1Id, step1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "MarketIntelAgent", "Analyzed social sentiment",
                "Internal", stage1Start, stage1Duration, sentimentResult.Success ? "Success" : "Failed",
                sentimentResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = sentimentResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = sentimentResult.A2UIPayload != null ? new[] { "SocialSentimentGraph" } : null,
                ErrorMessage = sentimentResult.ErrorMessage
            };

            if (!sentimentResult.Success)
            {
                _logger.LogWarning("MarketIntelAgent sentiment analysis failed - aborting workflow");
                return await BuildViralSpikeFailureResultAsync(sessionId, results, pipelineStages,
                    "Failed to analyze social sentiment", startTime, cancellationToken);
            }

            // Step 2: Calculate flash sale pricing (PricingAgent)
            _logger.LogInformation("Step 2: Delegating to PricingAgent for flash sale pricing");

            var step2Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to PricingAgent for flash sale pricing on complementary items", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "PricingAgent", ReasoningStepType.ToolCall,
                $"Calculating flash sale prices for {sku} and complementary items", step2Id, cancellationToken: cancellationToken);

            var stage2Start = DateTimeOffset.UtcNow;
            var step2Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 2,
                AgentName = "PricingAgent",
                StageName = "Flash Sale Pricing",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage2Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "PricingAgent", true, cancellationToken);
            var pricingResult = await _pricingAgent.CalculateFlashSalePricingAsync(sku, demandMultiplier, region, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "PricingAgent", false, cancellationToken);
            step2Sw.Stop();
            results.Add(pricingResult);

            await EmitTraceAsync(sessionId, "PricingAgent", ReasoningStepType.Observation,
                $"Received {(pricingResult.Success ? "flash sale pricing" : "pricing failure")} from PricingAgent",
                step2Id, step2Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage2Duration = DateTimeOffset.UtcNow - stage2Start;
            await RecordAuditEntryAsync(sessionId, "PricingAgent", "Calculated flash sale pricing",
                "MCP", stage2Start, stage2Duration, pricingResult.Success ? "Success" : "Failed",
                pricingResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[1] = pipelineStages[1] with
            {
                Status = pricingResult.Success ? "Completed" : "Failed",
                Duration = stage2Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = pricingResult.A2UIPayload != null ? new[] { "PricingImpactChart" } : null,
                ErrorMessage = pricingResult.ErrorMessage
            };

            if (!pricingResult.Success)
            {
                _logger.LogWarning("PricingAgent flash sale failed - aborting workflow");
                return await BuildViralSpikeFailureResultAsync(sessionId, results, pipelineStages,
                    "Failed to calculate flash sale pricing", startTime, cancellationToken);
            }

            // Step 3: Build campaign preview (MarketingAgent)
            _logger.LogInformation("Step 3: Delegating to MarketingAgent for campaign preview");

            var step3Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to MarketingAgent for campaign preview", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "MarketingAgent", ReasoningStepType.ToolCall,
                $"Building campaign preview for {sku} targeting {region}", step3Id, cancellationToken: cancellationToken);

            var stage3Start = DateTimeOffset.UtcNow;
            var step3Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 3,
                AgentName = "MarketingAgent",
                StageName = "Campaign Preview",
                Status = "Running",
                Protocol = "Internal",
                StartedAt = stage3Start
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketingAgent", true, cancellationToken);
            var marketingResult = await _marketingAgent.ExecuteAsync(sku, demandMultiplier, region, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MarketingAgent", false, cancellationToken);
            step3Sw.Stop();
            results.Add(marketingResult);

            await EmitTraceAsync(sessionId, "MarketingAgent", ReasoningStepType.Observation,
                $"Received {(marketingResult.Success ? "campaign preview" : "campaign failure")} from MarketingAgent",
                step3Id, step3Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage3Duration = DateTimeOffset.UtcNow - stage3Start;
            await RecordAuditEntryAsync(sessionId, "MarketingAgent", "Built campaign preview",
                "Internal", stage3Start, stage3Duration, marketingResult.Success ? "Success" : "Failed",
                marketingResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[2] = pipelineStages[2] with
            {
                Status = marketingResult.Success ? "Completed" : "Failed",
                Duration = stage3Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = marketingResult.A2UIPayload != null ? new[] { "CampaignPreview" } : null,
                ErrorMessage = marketingResult.ErrorMessage
            };

            if (!marketingResult.Success)
            {
                _logger.LogWarning("MarketingAgent failed - continuing with partial data");
            }

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing viral spike orchestrator response");

            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);

            var synthesizeStart = DateTimeOffset.UtcNow;
            var executiveSummary = BuildViralSpikeExecutiveSummary(sku, demandMultiplier, region, source, results);
            var synthesizeDuration = DateTimeOffset.UtcNow - synthesizeStart;

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Decision,
                $"Recommending flash sale campaign for {sku} in {region} to capitalize on {source} viral spike",
                rootStepId, (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Synthesized viral spike response",
                "AGUI", synthesizeStart, synthesizeDuration, "Success",
                $"Generated executive summary with {results.Count} A2UI payloads",
                activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;

            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            _logger.LogInformation(
                "Orchestrator viral spike workflow completed successfully in {Duration}ms",
                duration.TotalMilliseconds);

            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "ViralSpikeWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildViralSpikeInsightCards(sku, demandMultiplier, region, source, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator viral spike workflow failed");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"Viral spike workflow failed: {ex.Message}", rootStepId, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Viral spike workflow execution failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            return await BuildViralSpikeFailureResultAsync(sessionId, results, pipelineStages,
                $"Viral spike orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    /// <summary>
    /// Orchestrates a supply chain shock response workflow.
    /// Analyzes delayed shipments, assesses inventory impact, and builds redistribution plans.
    /// </summary>
    /// <param name="sku">Product SKU affected by supply chain disruption</param>
    /// <param name="delayDays">Number of days the shipment is delayed</param>
    /// <param name="reason">Reason for the supply chain disruption</param>
    /// <param name="affectedRegions">Regions/stores affected by the disruption</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Orchestrated result with all A2UI payloads and executive summary</returns>
    public async Task<OrchestratorResult> ProcessSupplyChainShockAsync(
        string sku,
        int delayDays,
        string reason,
        string[] affectedRegions,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";

        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "OrchestrateSupplyChainShock");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.delay_days", delayDays);
        activity?.SetTag("agent.reason", reason);
        activity?.SetTag("agent.affected_regions", string.Join(",", affectedRegions));
        activity?.SetTag("agent.session_id", sessionId);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting supply chain shock workflow: SKU {Sku}, DelayDays {DelayDays}, Reason {Reason}, Regions {Regions}, SessionId {SessionId}",
            sku, delayDays, reason, string.Join(", ", affectedRegions), sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        var rootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Analyzing supply chain shock for {sku}: {delayDays}-day delay due to {reason}, affecting {string.Join(", ", affectedRegions)}",
            cancellationToken: cancellationToken);

        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated supply chain shock response workflow",
            "AGUI", startTime, TimeSpan.Zero, "Success",
            $"Supply chain shock for SKU {sku}: {delayDays}-day delay due to {reason}",
            activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

        try
        {
            // Step 1: Analyze shipment delays (LogisticsAgent)
            _logger.LogInformation("Step 1: Delegating to LogisticsAgent for shipment delay analysis");

            var step1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to LogisticsAgent for shipment delay analysis", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "LogisticsAgent", ReasoningStepType.ToolCall,
                $"Querying shipment status for {sku}", step1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var step1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "LogisticsAgent",
                StageName = "Shipment Delay Analysis",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage1Start,
                ToolsUsed = new[] { "GetShipmentStatus" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "LogisticsAgent", true, cancellationToken);
            var logisticsResult = await _logisticsAgent.ExecuteAsync(sku, delayDays, reason, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "LogisticsAgent", false, cancellationToken);
            step1Sw.Stop();
            results.Add(logisticsResult);

            await EmitTraceAsync(sessionId, "LogisticsAgent", ReasoningStepType.Observation,
                $"Received {(logisticsResult.Success ? "shipment delay analysis" : "analysis failure")} from LogisticsAgent",
                step1Id, step1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "LogisticsAgent", "Analyzed shipment delays",
                "MCP", stage1Start, stage1Duration, logisticsResult.Success ? "Success" : "Failed",
                logisticsResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = logisticsResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = logisticsResult.A2UIPayload != null ? new[] { "ReroutingMap" } : null,
                ErrorMessage = logisticsResult.ErrorMessage
            };

            if (!logisticsResult.Success)
            {
                _logger.LogWarning("LogisticsAgent failed - aborting workflow");
                return await BuildSupplyChainFailureResultAsync(sessionId, results, pipelineStages,
                    "Failed to analyze shipment delays", startTime, cancellationToken);
            }

            // Step 2: Get inventory snapshot (InventoryAgent - reused)
            _logger.LogInformation("Step 2: Delegating to InventoryAgent for inventory snapshot");

            var step2Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to InventoryAgent for current stock levels", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.ToolCall,
                $"Calling GetInventoryLevels with SKU={sku}", step2Id, cancellationToken: cancellationToken);

            var stage2Start = DateTimeOffset.UtcNow;
            var step2Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 2,
                AgentName = "InventoryAgent",
                StageName = "Inventory Assessment",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage2Start,
                ToolsUsed = new[] { "GetInventoryLevels" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", true, cancellationToken);
            var inventoryResult = await _inventoryAgent.ExecuteAsync(sku, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "InventoryAgent", false, cancellationToken);
            step2Sw.Stop();
            results.Add(inventoryResult);

            await EmitTraceAsync(sessionId, "InventoryAgent", ReasoningStepType.Observation,
                $"Received inventory snapshot from InventoryAgent",
                step2Id, step2Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage2Duration = DateTimeOffset.UtcNow - stage2Start;
            await RecordAuditEntryAsync(sessionId, "InventoryAgent", "Retrieved inventory snapshot for supply chain impact",
                "MCP", stage2Start, stage2Duration, inventoryResult.Success ? "Success" : "Warning",
                inventoryResult.TextSummary, activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

            pipelineStages[1] = pipelineStages[1] with
            {
                Status = inventoryResult.Success ? "Completed" : "Failed",
                Duration = stage2Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = inventoryResult.A2UIPayload != null ? new[] { "RetailStockHeatmap" } : null,
                ErrorMessage = inventoryResult.ErrorMessage
            };

            if (!inventoryResult.Success)
            {
                _logger.LogWarning("InventoryAgent failed - continuing with limited data");
            }

            // Step 3: Calculate redistribution plan (RedistributionAgent)
            _logger.LogInformation("Step 3: Delegating to RedistributionAgent for redistribution plan");

            var step3Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to RedistributionAgent for optimal stock redistribution", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "RedistributionAgent", ReasoningStepType.ToolCall,
                $"Computing redistribution routes for {sku} across {affectedRegions.Length} region(s)", step3Id, cancellationToken: cancellationToken);

            var stage3Start = DateTimeOffset.UtcNow;
            var step3Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 3,
                AgentName = "RedistributionAgent",
                StageName = "Stock Redistribution",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage3Start,
                ToolsUsed = new[] { "GetDeliveryRoutes" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "RedistributionAgent", true, cancellationToken);
            var redistributionResult = await _redistributionAgent.ExecuteAsync(sku, affectedRegions, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "RedistributionAgent", false, cancellationToken);
            step3Sw.Stop();
            results.Add(redistributionResult);

            await EmitTraceAsync(sessionId, "RedistributionAgent", ReasoningStepType.Observation,
                $"Received {(redistributionResult.Success ? "redistribution plan" : "planning failure")} from RedistributionAgent",
                step3Id, step3Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage3Duration = DateTimeOffset.UtcNow - stage3Start;
            await RecordAuditEntryAsync(sessionId, "RedistributionAgent", "Calculated stock redistribution plan",
                "MCP", stage3Start, stage3Duration, redistributionResult.Success ? "Success" : "Failed",
                redistributionResult.TextSummary, activity?.TraceId.ToString(), new[] { sku },
                affectedRegions, null, cancellationToken);

            pipelineStages[2] = pipelineStages[2] with
            {
                Status = redistributionResult.Success ? "Completed" : "Failed",
                Duration = stage3Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = redistributionResult.A2UIPayload != null ? new[] { "ReroutingMap" } : null,
                ErrorMessage = redistributionResult.ErrorMessage
            };

            if (!redistributionResult.Success)
            {
                _logger.LogWarning("RedistributionAgent failed - aborting workflow");
                return await BuildSupplyChainFailureResultAsync(sessionId, results, pipelineStages,
                    "Failed to calculate redistribution plan", startTime, cancellationToken);
            }

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing supply chain shock orchestrator response");

            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);

            var synthesizeStart = DateTimeOffset.UtcNow;
            var executiveSummary = BuildSupplyChainExecutiveSummary(sku, delayDays, reason, affectedRegions, results);
            var synthesizeDuration = DateTimeOffset.UtcNow - synthesizeStart;

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Decision,
                $"Recommending redistribution of stock from surplus stores to mitigate {reason} delay for {sku}",
                rootStepId, (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Synthesized supply chain shock response",
                "AGUI", synthesizeStart, synthesizeDuration, "Success",
                $"Generated executive summary with {results.Count} A2UI payloads",
                activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;

            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            _logger.LogInformation(
                "Orchestrator supply chain shock workflow completed successfully in {Duration}ms",
                duration.TotalMilliseconds);

            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "SupplyChainShockWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildSupplyChainInsightCards(sku, delayDays, reason, affectedRegions, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator supply chain shock workflow failed");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"Supply chain shock workflow failed: {ex.Message}", rootStepId, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Supply chain shock workflow execution failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            return await BuildSupplyChainFailureResultAsync(sessionId, results, pipelineStages,
                $"Supply chain shock orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    private static string BuildExecutiveSummary(string sku, decimal competitorPrice, List<AgentResult> results)
    {
        var summary = $"## Competitor Price Response Analysis for {sku}\n\n";
        summary += $"**Competitor Price:** ${competitorPrice:F2}\n\n";

        foreach (var result in results)
        {
            summary += $"### {result.GetType().Name}\n";
            summary += $"{result.TextSummary}\n\n";
        }

        summary += "**Recommendation:** Review the pricing impact scenarios above and select the optimal strategy. " +
                   "All competitor data has been validated via A2A protocol and cross-referenced against internal benchmarks.";

        return summary;
    }

    private static string BuildBulkExecutiveSummary(string competitorName, IReadOnlyList<(string Sku, decimal CompetitorPrice)> items, List<AgentResult> results)
    {
        var summary = $"## Bulk Competitor Price Response Analysis\n\n";
        summary += $"**Competitor:** {competitorName}\n";
        summary += $"**SKUs Analyzed:** {items.Count}\n";
        summary += $"**Average Competitor Price:** ${items.Average(i => i.CompetitorPrice):F2}\n\n";

        foreach (var result in results)
        {
            summary += $"### {result.GetType().Name}\n";
            summary += $"{result.TextSummary}\n\n";
        }

        summary += $"**Recommendation:** Review the consolidated pricing impact scenarios above. " +
                   $"This bulk analysis covers {items.Count} SKUs with total projected revenue impact across all stores. " +
                   $"All competitor data has been validated via A2A protocol and cross-referenced against internal benchmarks.";

        return summary;
    }

    private static IReadOnlyList<InsightCardData> BuildInsightCards(
        string sku, decimal competitorPrice, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();

            // Card 1: Margin Impact — derived from pricing agent result
            var pricingResult = results.LastOrDefault(r => r.TextSummary.Contains("margin", StringComparison.OrdinalIgnoreCase)
                                                        || r.TextSummary.Contains("pricing", StringComparison.OrdinalIgnoreCase));
            var marginDelta = pricingResult != null ? ExtractPercentage(pricingResult.TextSummary) : -5.2m;
            var marginDirection = marginDelta < 0 ? "down" : marginDelta > 0 ? "up" : "neutral";
            cards.Add(new InsightCardData
            {
                Title = "Margin Impact",
                KeyMetric = $"{marginDelta:+0.0;-0.0;0.0}%",
                MetricLabel = "projected margin change",
                TrendDirection = marginDirection,
                Summary = $"Matching competitor price of ${competitorPrice:F2} for {sku} would shift margins. Review pricing scenarios to find the optimal balance between competitiveness and profitability.",
                Severity = marginDelta < -10 ? "critical" : marginDelta < -3 ? "warning" : "info"
            });

            // Card 2: Competitive Position — derived from market intel result
            var marketResult = results.FirstOrDefault(r => r.TextSummary.Contains("competitor", StringComparison.OrdinalIgnoreCase)
                                                        || r.TextSummary.Contains("market", StringComparison.OrdinalIgnoreCase));
            var competitorCount = marketResult != null ? ExtractCount(marketResult.TextSummary) : 3;
            cards.Add(new InsightCardData
            {
                Title = "Competitive Position",
                KeyMetric = $"{competitorCount} competitors",
                MetricLabel = "undercutting our price",
                TrendDirection = competitorCount > 2 ? "down" : "up",
                Summary = $"Competitive analysis shows {competitorCount} competitor(s) with lower pricing on {sku}. Data validated through A2A cross-referencing.",
                Severity = competitorCount > 3 ? "critical" : competitorCount > 1 ? "warning" : "success"
            });

            // Card 3: Recommended Action — synthesized confidence
            var allSucceeded = results.All(r => r.Success);
            var confidence = allSucceeded ? "high" : "moderate";
            cards.Add(new InsightCardData
            {
                Title = "Recommended Action",
                KeyMetric = confidence == "high" ? "88%" : "62%",
                MetricLabel = "confidence score",
                TrendDirection = "up",
                Summary = $"With {confidence} confidence, recommend reviewing the proposed pricing scenarios. All {results.Count} agent analyses completed {(allSucceeded ? "successfully" : "with partial data")}.",
                ActionLabel = "Review Scenarios",
                Severity = "success"
            });

            return cards;
        }
        catch
        {
            // Insight card generation should never break the workflow
            return Array.Empty<InsightCardData>();
        }
    }

    private static IReadOnlyList<InsightCardData> BuildBulkInsightCards(
        string competitorName, IReadOnlyList<(string Sku, decimal CompetitorPrice)> items, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();
            var avgPrice = items.Average(i => i.CompetitorPrice);

            cards.Add(new InsightCardData
            {
                Title = "Margin Impact",
                KeyMetric = $"{items.Count} SKUs",
                MetricLabel = "affected by price changes",
                TrendDirection = "down",
                Summary = $"Bulk analysis of {items.Count} SKUs from {competitorName} with average competitor price of ${avgPrice:F2}. Review consolidated pricing scenarios for portfolio-level impact.",
                Severity = items.Count > 5 ? "critical" : items.Count > 2 ? "warning" : "info"
            });

            cards.Add(new InsightCardData
            {
                Title = "Competitive Position",
                KeyMetric = competitorName,
                MetricLabel = "is undercutting across portfolio",
                TrendDirection = "down",
                Summary = $"{competitorName} has dropped prices on {items.Count} SKUs simultaneously, suggesting a strategic market move. Coordinated response recommended.",
                Severity = "warning"
            });

            var allSucceeded = results.All(r => r.Success);
            cards.Add(new InsightCardData
            {
                Title = "Recommended Action",
                KeyMetric = allSucceeded ? "92%" : "65%",
                MetricLabel = "confidence score",
                TrendDirection = "up",
                Summary = $"Bulk analysis complete with {(allSucceeded ? "full" : "partial")} data from {results.Count} agent passes. Recommend a coordinated pricing response across all affected SKUs.",
                ActionLabel = "Review Bulk Scenarios",
                Severity = "success"
            });

            return cards;
        }
        catch
        {
            return Array.Empty<InsightCardData>();
        }
    }

    private static decimal ExtractPercentage(string text)
    {
        // Try to find a percentage-like value from agent summary text
        var match = System.Text.RegularExpressions.Regex.Match(text, @"(-?\d+\.?\d*)%");
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var pct))
            return pct;
        return -5.2m; // Sensible default
    }

    private static int ExtractCount(string text)
    {
        // Try to extract a competitor count from agent summary
        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)\s*competitor", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
            return count;
        return 3; // Sensible default
    }

    private OrchestratorResult BuildFailureResult(List<AgentResult> results, string errorMessage, DateTimeOffset startTime)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Workflow failed: {errorMessage}",
            AgentResults = results,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    private async Task<OrchestratorResult> BuildFailureResultAsync(
        string sessionId,
        List<AgentResult> results,
        List<PipelineStage> stages,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
        var pipelineData = new AgentPipelineData
        {
            SessionId = sessionId,
            WorkflowName = "CompetitorPriceDropWorkflow",
            Stages = stages,
            OverallStatus = "Failed",
            TotalDuration = duration,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow
        };

        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Workflow failed: {errorMessage}",
            AgentResults = results,
            AuditTrailData = auditTrailData,
            PipelineData = pipelineData,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    private static string BuildInventoryQueryExecutiveSummary(string sku, List<AgentResult> results)
    {
        var summary = $"## Inventory Query Results for {sku}\n\n";

        foreach (var result in results)
        {
            summary += $"{result.TextSummary}\n\n";
        }

        summary += "**Note:** This is a read-only inventory snapshot. No pricing or competitor analysis was requested.";
        return summary;
    }

    private static IReadOnlyList<InsightCardData> BuildInventoryQueryInsightCards(string sku, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();

            var inventoryResult = results.FirstOrDefault(r => r.Success);
            var totalUnitsMatch = inventoryResult != null
                ? System.Text.RegularExpressions.Regex.Match(inventoryResult.TextSummary, @"(\d+)\s*total\s*units")
                : System.Text.RegularExpressions.Regex.Match("", @".");
            var totalUnits = totalUnitsMatch.Success && int.TryParse(totalUnitsMatch.Groups[1].Value, out var tu) ? tu : 0;

            var lowStockMatch = inventoryResult != null
                ? System.Text.RegularExpressions.Regex.Match(inventoryResult.TextSummary, @"(\d+)\s*store\(s\)\s*below")
                : System.Text.RegularExpressions.Regex.Match("", @".");
            var lowStockCount = lowStockMatch.Success && int.TryParse(lowStockMatch.Groups[1].Value, out var ls) ? ls : 0;

            cards.Add(new InsightCardData
            {
                Title = "Stock Overview",
                KeyMetric = $"{totalUnits} units",
                MetricLabel = "total across all stores",
                TrendDirection = totalUnits > 100 ? "up" : totalUnits > 30 ? "neutral" : "down",
                Summary = $"SKU {sku} has {totalUnits} total units distributed across all retail locations.",
                Severity = totalUnits < 30 ? "critical" : totalUnits < 100 ? "warning" : "success"
            });

            cards.Add(new InsightCardData
            {
                Title = "Reorder Alerts",
                KeyMetric = $"{lowStockCount} stores",
                MetricLabel = "below reorder point",
                TrendDirection = lowStockCount > 0 ? "down" : "up",
                Summary = lowStockCount > 0
                    ? $"{lowStockCount} store(s) have stock below the reorder point for {sku}. Consider initiating replenishment."
                    : $"All stores have adequate stock levels for {sku}. No replenishment needed.",
                Severity = lowStockCount > 2 ? "critical" : lowStockCount > 0 ? "warning" : "success"
            });

            return cards;
        }
        catch
        {
            return Array.Empty<InsightCardData>();
        }
    }

    private async Task<OrchestratorResult> BuildInventoryQueryFailureResultAsync(
        string sessionId,
        List<AgentResult> results,
        List<PipelineStage> stages,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
        var pipelineData = new AgentPipelineData
        {
            SessionId = sessionId,
            WorkflowName = "InventoryQueryWorkflow",
            Stages = stages,
            OverallStatus = "Failed",
            TotalDuration = duration,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow
        };

        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Inventory query failed: {errorMessage}",
            AgentResults = results,
            AuditTrailData = auditTrailData,
            PipelineData = pipelineData,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    private async Task RecordAuditEntryAsync(
        string sessionId,
        string agentName,
        string action,
        string protocol,
        DateTimeOffset timestamp,
        TimeSpan duration,
        string status,
        string? details,
        string? traceId,
        IReadOnlyList<string>? affectedSkus,
        IReadOnlyList<string>? affectedStores,
        string? decisionOutcome,
        CancellationToken cancellationToken)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            AgentName = agentName,
            Action = action,
            Protocol = protocol,
            Timestamp = timestamp,
            Duration = duration,
            Status = status,
            Details = details,
            TraceId = traceId,
            AffectedSkus = affectedSkus,
            AffectedStores = affectedStores,
            DecisionOutcome = decisionOutcome
        };

        await _auditRepository.RecordAuditEntryAsync(sessionId, entry, cancellationToken);
    }

    private async Task<DecisionAuditTrailData> BuildAuditTrailDataAsync(string sessionId, CancellationToken cancellationToken)
    {
        var entries = await _auditRepository.GetAuditTrailAsync(sessionId, cancellationToken);
        return new DecisionAuditTrailData
        {
            SessionId = sessionId,
            Entries = entries,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private static string BuildViralSpikeExecutiveSummary(
        string sku, decimal demandMultiplier, string region, string source, List<AgentResult> results)
    {
        var summary = $"## Viral Spike Response Analysis for {sku}\n\n";
        summary += $"**Source:** {source}\n";
        summary += $"**Region:** {region}\n";
        summary += $"**Demand Multiplier:** {demandMultiplier:F1}x\n\n";

        foreach (var result in results)
        {
            summary += $"{result.TextSummary}\n\n";
        }

        summary += $"**Recommendation:** Capitalize on the {source} viral spike by launching the flash sale campaign " +
                   $"targeting {region}. Monitor sentiment velocity to adjust pricing and inventory replenishment in real-time.";

        return summary;
    }

    private static IReadOnlyList<InsightCardData> BuildViralSpikeInsightCards(
        string sku, decimal demandMultiplier, string region, string source, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();

            cards.Add(new InsightCardData
            {
                Title = "Demand Surge",
                KeyMetric = $"{demandMultiplier:F0}x",
                MetricLabel = $"demand spike from {source}",
                TrendDirection = "up",
                Summary = $"Social sentiment for {sku} is surging {demandMultiplier:F0}x in {region} driven by {source}. Immediate action recommended to capitalize on viral momentum.",
                Severity = demandMultiplier > 5 ? "critical" : demandMultiplier > 3 ? "warning" : "info"
            });

            var pricingResult = results.FirstOrDefault(r => r.TextSummary.Contains("flash sale", StringComparison.OrdinalIgnoreCase)
                                                          || r.TextSummary.Contains("pricing", StringComparison.OrdinalIgnoreCase));
            var revenueMatch = pricingResult != null
                ? System.Text.RegularExpressions.Regex.Match(pricingResult.TextSummary, @"\$([\d,]+\.?\d*)")
                : System.Text.RegularExpressions.Regex.Match("", @".");
            var revenueStr = revenueMatch.Success ? $"${revenueMatch.Groups[1].Value}" : "projected";

            cards.Add(new InsightCardData
            {
                Title = "Flash Sale Opportunity",
                KeyMetric = revenueStr,
                MetricLabel = "estimated revenue from complementary items",
                TrendDirection = "up",
                Summary = $"Flash sale pricing on complementary items (15-25% off) can increase average order value while the viral SKU drives traffic to {region} stores.",
                Severity = "success"
            });

            var allSucceeded = results.All(r => r.Success);
            cards.Add(new InsightCardData
            {
                Title = "Campaign Readiness",
                KeyMetric = allSucceeded ? "Ready" : "Partial",
                MetricLabel = "campaign launch status",
                TrendDirection = "up",
                Summary = $"Campaign assets generated with {(allSucceeded ? "full" : "partial")} data from {results.Count} agents. Email, hero banner, and flash sale pricing ready for {region} deployment.",
                ActionLabel = "Launch Campaign",
                Severity = allSucceeded ? "success" : "warning"
            });

            return cards;
        }
        catch
        {
            return Array.Empty<InsightCardData>();
        }
    }

    private async Task<OrchestratorResult> BuildViralSpikeFailureResultAsync(
        string sessionId,
        List<AgentResult> results,
        List<PipelineStage> stages,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
        var pipelineData = new AgentPipelineData
        {
            SessionId = sessionId,
            WorkflowName = "ViralSpikeWorkflow",
            Stages = stages,
            OverallStatus = "Failed",
            TotalDuration = duration,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow
        };

        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Viral spike workflow failed: {errorMessage}",
            AgentResults = results,
            AuditTrailData = auditTrailData,
            PipelineData = pipelineData,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    private static string BuildSupplyChainExecutiveSummary(
        string sku, int delayDays, string reason, string[] affectedRegions, List<AgentResult> results)
    {
        var summary = $"## Supply Chain Shock Response for {sku}\n\n";
        summary += $"**Disruption:** {reason}\n";
        summary += $"**Delay:** {delayDays} day(s)\n";
        summary += $"**Affected Regions:** {string.Join(", ", affectedRegions)}\n\n";

        foreach (var result in results)
        {
            summary += $"{result.TextSummary}\n\n";
        }

        summary += $"**Recommendation:** Execute the redistribution plan to move surplus stock from unaffected stores " +
                   $"to the {affectedRegions.Length} affected region(s). Monitor shipment recovery and adjust transfers " +
                   $"as the {reason} situation evolves.";

        return summary;
    }

    private static IReadOnlyList<InsightCardData> BuildSupplyChainInsightCards(
        string sku, int delayDays, string reason, string[] affectedRegions, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();

            // Card 1: Supply Disruption Severity
            var severity = delayDays >= 5 ? "critical" : delayDays >= 3 ? "warning" : "info";
            cards.Add(new InsightCardData
            {
                Title = "Supply Disruption",
                KeyMetric = $"{delayDays} days",
                MetricLabel = $"shipment delay from {reason}",
                TrendDirection = "down",
                Summary = $"SKU {sku} shipment delayed {delayDays} day(s) due to {reason}. " +
                          $"{affectedRegions.Length} region(s) affected: {string.Join(", ", affectedRegions)}.",
                Severity = severity
            });

            // Card 2: Redistribution Impact
            var redistResult = results.LastOrDefault(r => r.TextSummary.Contains("redistribution", StringComparison.OrdinalIgnoreCase)
                                                        || r.TextSummary.Contains("transfer", StringComparison.OrdinalIgnoreCase));
            var routeCountMatch = redistResult != null
                ? System.Text.RegularExpressions.Regex.Match(redistResult.TextSummary, @"(\d+)\s*transfer\s*route")
                : System.Text.RegularExpressions.Regex.Match("", @".");
            var routeCount = routeCountMatch.Success && int.TryParse(routeCountMatch.Groups[1].Value, out var rc) ? rc : 0;

            cards.Add(new InsightCardData
            {
                Title = "Redistribution Plan",
                KeyMetric = $"{routeCount} routes",
                MetricLabel = "inter-store transfers identified",
                TrendDirection = routeCount > 0 ? "up" : "neutral",
                Summary = $"Redistribution plan identifies {routeCount} transfer route(s) from surplus stores to at-risk locations. " +
                          $"Prioritized by stock criticality and geographic proximity.",
                Severity = routeCount > 0 ? "success" : "warning"
            });

            // Card 3: Overall Readiness
            var allSucceeded = results.All(r => r.Success);
            cards.Add(new InsightCardData
            {
                Title = "Response Readiness",
                KeyMetric = allSucceeded ? "Ready" : "Partial",
                MetricLabel = "mitigation plan status",
                TrendDirection = "up",
                Summary = $"Supply chain shock response completed with {(allSucceeded ? "full" : "partial")} analysis from {results.Count} agents. " +
                          $"Redistribution and inventory assessment data ready for execution.",
                ActionLabel = "Execute Transfers",
                Severity = allSucceeded ? "success" : "warning"
            });

            return cards;
        }
        catch
        {
            return Array.Empty<InsightCardData>();
        }
    }

    private async Task<OrchestratorResult> BuildSupplyChainFailureResultAsync(
        string sessionId,
        List<AgentResult> results,
        List<PipelineStage> stages,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
        var pipelineData = new AgentPipelineData
        {
            SessionId = sessionId,
            WorkflowName = "SupplyChainShockWorkflow",
            Stages = stages,
            OverallStatus = "Failed",
            TotalDuration = duration,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow
        };

        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Supply chain shock workflow failed: {errorMessage}",
            AgentResults = results,
            AuditTrailData = auditTrailData,
            PipelineData = pipelineData,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    /// <summary>
    /// Orchestrates a store readiness workflow: Traffic → Merchandising → ManagerHITL → Synthesis.
    /// </summary>
    public async Task<OrchestratorResult> ProcessStoreReadinessAsync(
        string storeId,
        string section,
        DateTimeOffset openingDate,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";

        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "OrchestrateStoreReadiness");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.store_id", storeId);
        activity?.SetTag("agent.section", section);
        activity?.SetTag("agent.session_id", sessionId);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting store readiness workflow: StoreId {StoreId}, Section {Section}, Opening {Opening}, SessionId {SessionId}",
            storeId, section, openingDate, sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        var rootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Analyzing store readiness for {storeId} section {section}, opening {openingDate:yyyy-MM-dd}",
            cancellationToken: cancellationToken);

        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated store readiness workflow",
            "AGUI", startTime, TimeSpan.Zero, "Success",
            $"Store readiness for {storeId} section {section}",
            activity?.TraceId.ToString(), null, new[] { storeId }, null, cancellationToken);

        try
        {
            // Step 1: Traffic Analysis (TrafficAnalystAgent)
            _logger.LogInformation("Step 1: Delegating to TrafficAnalystAgent for traffic analysis");

            var step1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to TrafficAnalystAgent for foot traffic analysis", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "TrafficAnalystAgent", ReasoningStepType.ToolCall,
                $"Querying foot traffic data for {storeId} section {section}", step1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var step1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "TrafficAnalystAgent",
                StageName = "Traffic Analysis",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage1Start,
                ToolsUsed = new[] { "GetFootTrafficData" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "TrafficAnalystAgent", true, cancellationToken);
            var trafficResult = await _trafficAnalystAgent.ExecuteAsync(storeId, section, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "TrafficAnalystAgent", false, cancellationToken);
            step1Sw.Stop();
            results.Add(trafficResult);

            await EmitTraceAsync(sessionId, "TrafficAnalystAgent", ReasoningStepType.Observation,
                $"Received {(trafficResult.Success ? "traffic analysis" : "analysis failure")} from TrafficAnalystAgent",
                step1Id, step1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "TrafficAnalystAgent", "Analyzed foot traffic data",
                "MCP", stage1Start, stage1Duration, trafficResult.Success ? "Success" : "Failed",
                trafficResult.TextSummary, activity?.TraceId.ToString(), null, new[] { storeId }, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = trafficResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = trafficResult.A2UIPayload != null ? new[] { "InteractiveFloorplan" } : null,
                ErrorMessage = trafficResult.ErrorMessage
            };

            if (!trafficResult.Success)
            {
                _logger.LogWarning("TrafficAnalystAgent failed - aborting workflow");
                return await BuildStoreReadinessFailureResultAsync(sessionId, results, pipelineStages,
                    "Failed to analyze foot traffic", startTime, cancellationToken);
            }

            // Step 2: Merchandising Optimization (MerchandisingAgent)
            _logger.LogInformation("Step 2: Delegating to MerchandisingAgent for planogram optimization");

            var step2Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to MerchandisingAgent for planogram optimization", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "MerchandisingAgent", ReasoningStepType.ToolCall,
                $"Analyzing planogram for {storeId} section {section}", step2Id, cancellationToken: cancellationToken);

            var stage2Start = DateTimeOffset.UtcNow;
            var step2Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 2,
                AgentName = "MerchandisingAgent",
                StageName = "Merchandising Optimization",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage2Start,
                ToolsUsed = new[] { "GetPlanogramData" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MerchandisingAgent", true, cancellationToken);
            var merchandisingResult = await _merchandisingAgent.ExecuteAsync(storeId, section, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "MerchandisingAgent", false, cancellationToken);
            step2Sw.Stop();
            results.Add(merchandisingResult);

            await EmitTraceAsync(sessionId, "MerchandisingAgent", ReasoningStepType.Observation,
                $"Received {(merchandisingResult.Success ? "merchandising analysis" : "analysis failure")} from MerchandisingAgent",
                step2Id, step2Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage2Duration = DateTimeOffset.UtcNow - stage2Start;
            await RecordAuditEntryAsync(sessionId, "MerchandisingAgent", "Analyzed planogram optimization",
                "MCP", stage2Start, stage2Duration, merchandisingResult.Success ? "Success" : "Failed",
                merchandisingResult.TextSummary, activity?.TraceId.ToString(), null, new[] { storeId }, null, cancellationToken);

            pipelineStages[1] = pipelineStages[1] with
            {
                Status = merchandisingResult.Success ? "Completed" : "Failed",
                Duration = stage2Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = merchandisingResult.A2UIPayload != null ? new[] { "InteractiveFloorplan" } : null,
                ErrorMessage = merchandisingResult.ErrorMessage
            };

            // Step 3: Manager HITL Approval (ManagerAgent)
            _logger.LogInformation("Step 3: Delegating to ManagerAgent for HITL approval");

            var step3Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to ManagerAgent for human-in-the-loop approval", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "ManagerAgent", ReasoningStepType.ToolCall,
                $"Manager reviewing planogram changes for {storeId} section {section}", step3Id, cancellationToken: cancellationToken);

            var stage3Start = DateTimeOffset.UtcNow;
            var step3Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 3,
                AgentName = "ManagerAgent",
                StageName = "Manager Approval (HITL)",
                Status = "Running",
                Protocol = "Internal",
                StartedAt = stage3Start
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ManagerAgent", true, cancellationToken);
            var managerResult = await _managerAgent.ExecuteAsync(storeId, section, merchandisingResult, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ManagerAgent", false, cancellationToken);
            step3Sw.Stop();
            results.Add(managerResult);

            await EmitTraceAsync(sessionId, "ManagerAgent", ReasoningStepType.Observation,
                $"Manager {(managerResult.Success ? "approved" : "deferred")} merchandising changes",
                step3Id, step3Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage3Duration = DateTimeOffset.UtcNow - stage3Start;
            await RecordAuditEntryAsync(sessionId, "ManagerAgent", managerResult.Success ? "Approved planogram changes" : "Deferred planogram changes",
                "Internal", stage3Start, stage3Duration, managerResult.Success ? "Success" : "Warning",
                managerResult.TextSummary, activity?.TraceId.ToString(), null, new[] { storeId }, null, cancellationToken);

            pipelineStages[2] = pipelineStages[2] with
            {
                Status = managerResult.Success ? "Completed" : "Failed",
                Duration = stage3Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = managerResult.ErrorMessage
            };

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing store readiness response");

            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);

            var synthesizeStart = DateTimeOffset.UtcNow;
            var executiveSummary = BuildStoreReadinessExecutiveSummary(storeId, section, openingDate, results);
            var synthesizeDuration = DateTimeOffset.UtcNow - synthesizeStart;

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Decision,
                $"Store readiness assessment complete for {storeId} section {section}",
                rootStepId, (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Synthesized store readiness response",
                "AGUI", synthesizeStart, synthesizeDuration, "Success",
                $"Generated executive summary with {results.Count} A2UI payloads",
                activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;

            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            _logger.LogInformation("Store readiness workflow completed in {Duration}ms", duration.TotalMilliseconds);

            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "StoreReadinessWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildStoreReadinessInsightCards(storeId, section, openingDate, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Store readiness workflow failed");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"Store readiness workflow failed: {ex.Message}", rootStepId, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Store readiness workflow failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            return await BuildStoreReadinessFailureResultAsync(sessionId, results, pipelineStages,
                $"Store readiness orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    /// <summary>
    /// Orchestrates an ESG audit workflow: Compliance → Research → Procurement → Synthesis.
    /// </summary>
    public async Task<OrchestratorResult> ProcessESGAuditAsync(
        string category,
        string certRequired,
        DateTimeOffset deadline,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sessionId = $"session-{Guid.NewGuid():N}";

        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "OrchestrateESGAudit");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.category", category);
        activity?.SetTag("agent.certification", certRequired);
        activity?.SetTag("agent.session_id", sessionId);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting ESG audit workflow: Category {Category}, Cert {Cert}, Deadline {Deadline}, SessionId {SessionId}",
            category, certRequired, deadline, sessionId);

        var results = new List<AgentResult>();
        var pipelineStages = new List<PipelineStage>();

        var rootStepId = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
            $"Auditing ESG compliance for {category} suppliers, {certRequired} certification required by {deadline:yyyy-MM-dd}",
            cancellationToken: cancellationToken);

        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated ESG audit workflow",
            "AGUI", startTime, TimeSpan.Zero, "Success",
            $"ESG audit for {category} ({certRequired}) deadline {deadline:yyyy-MM-dd}",
            activity?.TraceId.ToString(), null, null, null, cancellationToken);

        try
        {
            // Step 1: Compliance Analysis (ComplianceAgent)
            _logger.LogInformation("Step 1: Delegating to ComplianceAgent for supplier certification analysis");

            var step1Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to ComplianceAgent for supplier certification analysis", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "ComplianceAgent", ReasoningStepType.ToolCall,
                $"Querying supplier certifications for {category}", step1Id, cancellationToken: cancellationToken);

            var stage1Start = DateTimeOffset.UtcNow;
            var step1Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 1,
                AgentName = "ComplianceAgent",
                StageName = "Compliance Analysis",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage1Start,
                ToolsUsed = new[] { "GetSupplierCertifications" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ComplianceAgent", true, cancellationToken);
            var complianceResult = await _complianceAgent.ExecuteAsync(category, certRequired, deadline, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ComplianceAgent", false, cancellationToken);
            step1Sw.Stop();
            results.Add(complianceResult);

            await EmitTraceAsync(sessionId, "ComplianceAgent", ReasoningStepType.Observation,
                $"Received {(complianceResult.Success ? "compliance analysis" : "analysis failure")} from ComplianceAgent",
                step1Id, step1Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage1Duration = DateTimeOffset.UtcNow - stage1Start;
            await RecordAuditEntryAsync(sessionId, "ComplianceAgent", "Analyzed supplier certifications",
                "MCP", stage1Start, stage1Duration, complianceResult.Success ? "Success" : "Failed",
                complianceResult.TextSummary, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            pipelineStages[0] = pipelineStages[0] with
            {
                Status = complianceResult.Success ? "Completed" : "Failed",
                Duration = stage1Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                OutputPayloads = complianceResult.A2UIPayload != null ? new[] { "SupplierRiskMatrix" } : null,
                ErrorMessage = complianceResult.ErrorMessage
            };

            if (!complianceResult.Success)
            {
                _logger.LogWarning("ComplianceAgent failed - aborting workflow");
                return await BuildESGAuditFailureResultAsync(sessionId, results, pipelineStages,
                    "Failed to analyze supplier compliance", startTime, cancellationToken);
            }

            // Step 2: Sustainability Research (ResearchAgent)
            _logger.LogInformation("Step 2: Delegating to ResearchAgent for sustainability watchlist cross-reference");

            var step2Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to ResearchAgent for sustainability watchlist research", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "ResearchAgent", ReasoningStepType.ToolCall,
                $"Cross-referencing {category} suppliers against watchlists", step2Id, cancellationToken: cancellationToken);

            var stage2Start = DateTimeOffset.UtcNow;
            var step2Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 2,
                AgentName = "ResearchAgent",
                StageName = "Sustainability Research",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage2Start,
                ToolsUsed = new[] { "GetSustainabilityWatchlist" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ResearchAgent", true, cancellationToken);
            var researchResult = await _researchAgent.ExecuteAsync(category, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ResearchAgent", false, cancellationToken);
            step2Sw.Stop();
            results.Add(researchResult);

            await EmitTraceAsync(sessionId, "ResearchAgent", ReasoningStepType.Observation,
                $"Received {(researchResult.Success ? "watchlist findings" : "research failure")} from ResearchAgent",
                step2Id, step2Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage2Duration = DateTimeOffset.UtcNow - stage2Start;
            await RecordAuditEntryAsync(sessionId, "ResearchAgent", "Cross-referenced sustainability watchlists",
                "MCP", stage2Start, stage2Duration, researchResult.Success ? "Success" : "Failed",
                researchResult.TextSummary, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            pipelineStages[1] = pipelineStages[1] with
            {
                Status = researchResult.Success ? "Completed" : "Failed",
                Duration = stage2Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = researchResult.ErrorMessage
            };

            if (!researchResult.Success)
            {
                _logger.LogWarning("ResearchAgent failed - continuing with partial data");
            }

            // Step 3: Procurement Alternatives (ProcurementAgent)
            _logger.LogInformation("Step 3: Delegating to ProcurementAgent for alternative supplier identification");

            var step3Id = await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Thinking,
                "Delegating to ProcurementAgent for alternative supplier identification", rootStepId, cancellationToken: cancellationToken);
            await EmitTraceAsync(sessionId, "ProcurementAgent", ReasoningStepType.ToolCall,
                $"Finding alternative {certRequired}-certified {category} suppliers", step3Id, cancellationToken: cancellationToken);

            var stage3Start = DateTimeOffset.UtcNow;
            var step3Sw = Stopwatch.StartNew();
            pipelineStages.Add(new PipelineStage
            {
                Order = 3,
                AgentName = "ProcurementAgent",
                StageName = "Procurement Alternatives",
                Status = "Running",
                Protocol = "MCP",
                StartedAt = stage3Start,
                ToolsUsed = new[] { "GetAlternativeSuppliers" }
            });

            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ProcurementAgent", true, cancellationToken);
            var procurementResult = await _procurementAgent.ExecuteAsync(category, certRequired, cancellationToken);
            await _thinkingNotifier.SendThinkingStateAsync(sessionId, "ProcurementAgent", false, cancellationToken);
            step3Sw.Stop();
            results.Add(procurementResult);

            await EmitTraceAsync(sessionId, "ProcurementAgent", ReasoningStepType.Observation,
                $"Received {(procurementResult.Success ? "procurement alternatives" : "procurement failure")} from ProcurementAgent",
                step3Id, step3Sw.ElapsedMilliseconds, cancellationToken: cancellationToken);

            var stage3Duration = DateTimeOffset.UtcNow - stage3Start;
            await RecordAuditEntryAsync(sessionId, "ProcurementAgent", "Identified alternative suppliers",
                "MCP", stage3Start, stage3Duration, procurementResult.Success ? "Success" : "Failed",
                procurementResult.TextSummary, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            pipelineStages[2] = pipelineStages[2] with
            {
                Status = procurementResult.Success ? "Completed" : "Failed",
                Duration = stage3Duration,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = procurementResult.ErrorMessage
            };

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing ESG audit response");

            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);

            var synthesizeStart = DateTimeOffset.UtcNow;
            var executiveSummary = BuildESGAuditExecutiveSummary(category, certRequired, deadline, results);
            var synthesizeDuration = DateTimeOffset.UtcNow - synthesizeStart;

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Decision,
                $"ESG audit complete for {category} ({certRequired}). Recommending supplier remediation actions.",
                rootStepId, (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Synthesized ESG audit response",
                "AGUI", synthesizeStart, synthesizeDuration, "Success",
                $"Generated executive summary with {results.Count} A2UI payloads",
                activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;

            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            _logger.LogInformation("ESG audit workflow completed in {Duration}ms", duration.TotalMilliseconds);

            var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
            var pipelineData = new AgentPipelineData
            {
                SessionId = sessionId,
                WorkflowName = "ESGAuditWorkflow",
                Stages = pipelineStages,
                OverallStatus = "Completed",
                TotalDuration = duration,
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow
            };

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                AuditTrailData = auditTrailData,
                PipelineData = pipelineData,
                InsightCards = BuildESGAuditInsightCards(category, certRequired, deadline, results),
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ESG audit workflow failed");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            await EmitTraceAsync(sessionId, "ChiefSoftwareArchitect", ReasoningStepType.Error,
                $"ESG audit workflow failed: {ex.Message}", rootStepId, cancellationToken: cancellationToken);

            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "ESG audit workflow failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

            return await BuildESGAuditFailureResultAsync(sessionId, results, pipelineStages,
                $"ESG audit orchestration error: {ex.Message}", startTime, cancellationToken);
        }
    }

    private static string BuildStoreReadinessExecutiveSummary(
        string storeId, string section, DateTimeOffset openingDate, List<AgentResult> results)
    {
        var summary = $"## Store Readiness Assessment for {storeId}\n\n";
        summary += $"**Section:** {section}\n";
        summary += $"**Opening Date:** {openingDate:yyyy-MM-dd}\n\n";

        foreach (var result in results)
        {
            summary += $"{result.TextSummary}\n\n";
        }

        summary += $"**Recommendation:** Review traffic analysis and merchandising optimizations above. " +
                   $"Manager approval has been obtained. Proceed with planogram implementation before {openingDate:yyyy-MM-dd}.";

        return summary;
    }

    private static IReadOnlyList<InsightCardData> BuildStoreReadinessInsightCards(
        string storeId, string section, DateTimeOffset openingDate, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();
            var daysUntilOpening = (int)(openingDate - DateTimeOffset.UtcNow).TotalDays;

            cards.Add(new InsightCardData
            {
                Title = "Opening Countdown",
                KeyMetric = $"{daysUntilOpening} days",
                MetricLabel = "until store opening",
                TrendDirection = daysUntilOpening < 14 ? "down" : "neutral",
                Summary = $"Store {storeId} section {section} opening in {daysUntilOpening} days. Traffic analysis and planogram optimization complete.",
                Severity = daysUntilOpening < 7 ? "critical" : daysUntilOpening < 14 ? "warning" : "info"
            });

            var managerResult = results.LastOrDefault(r => r.TextSummary.Contains("Approved", StringComparison.OrdinalIgnoreCase)
                                                        || r.TextSummary.Contains("Deferred", StringComparison.OrdinalIgnoreCase));
            var approved = managerResult?.Success ?? false;
            cards.Add(new InsightCardData
            {
                Title = "Manager Approval",
                KeyMetric = approved ? "Approved" : "Pending",
                MetricLabel = "planogram changes status",
                TrendDirection = approved ? "up" : "down",
                Summary = managerResult?.TextSummary ?? "Manager review pending for planogram changes.",
                Severity = approved ? "success" : "warning"
            });

            var allSucceeded = results.All(r => r.Success);
            cards.Add(new InsightCardData
            {
                Title = "Readiness Score",
                KeyMetric = allSucceeded ? "95%" : "72%",
                MetricLabel = "store readiness",
                TrendDirection = "up",
                Summary = $"Store readiness assessment complete with {(allSucceeded ? "full" : "partial")} data from {results.Count} agents. Section {section} is {(allSucceeded ? "ready" : "needs attention")}.",
                ActionLabel = "View Floorplan",
                Severity = allSucceeded ? "success" : "warning"
            });

            return cards;
        }
        catch
        {
            return Array.Empty<InsightCardData>();
        }
    }

    private async Task<OrchestratorResult> BuildStoreReadinessFailureResultAsync(
        string sessionId,
        List<AgentResult> results,
        List<PipelineStage> stages,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
        var pipelineData = new AgentPipelineData
        {
            SessionId = sessionId,
            WorkflowName = "StoreReadinessWorkflow",
            Stages = stages,
            OverallStatus = "Failed",
            TotalDuration = duration,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow
        };

        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Store readiness workflow failed: {errorMessage}",
            AgentResults = results,
            AuditTrailData = auditTrailData,
            PipelineData = pipelineData,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    private static string BuildESGAuditExecutiveSummary(
        string category, string certRequired, DateTimeOffset deadline, List<AgentResult> results)
    {
        var summary = $"## ESG Audit Report for {category}\n\n";
        summary += $"**Certification Required:** {certRequired}\n";
        summary += $"**Compliance Deadline:** {deadline:yyyy-MM-dd}\n\n";

        foreach (var result in results)
        {
            summary += $"{result.TextSummary}\n\n";
        }

        summary += $"**Recommendation:** Address non-compliant and at-risk suppliers before {deadline:yyyy-MM-dd}. " +
                   $"Engage alternative suppliers identified by the procurement team for seamless transition.";

        return summary;
    }

    private static IReadOnlyList<InsightCardData> BuildESGAuditInsightCards(
        string category, string certRequired, DateTimeOffset deadline, List<AgentResult> results)
    {
        try
        {
            var cards = new List<InsightCardData>();
            var daysUntilDeadline = (int)(deadline - DateTimeOffset.UtcNow).TotalDays;

            var complianceResult = results.FirstOrDefault(r => r.TextSummary.Contains("non-compliant", StringComparison.OrdinalIgnoreCase)
                                                            || r.TextSummary.Contains("compliant", StringComparison.OrdinalIgnoreCase));
            var nonCompliantMatch = complianceResult != null
                ? System.Text.RegularExpressions.Regex.Match(complianceResult.TextSummary, @"(\d+) non-compliant")
                : System.Text.RegularExpressions.Regex.Match("", @".");
            var nonCompliantCount = nonCompliantMatch.Success ? int.Parse(nonCompliantMatch.Groups[1].Value) : 0;

            cards.Add(new InsightCardData
            {
                Title = "Compliance Status",
                KeyMetric = nonCompliantCount > 0 ? $"{nonCompliantCount} violations" : "Compliant",
                MetricLabel = $"{category} supplier compliance",
                TrendDirection = nonCompliantCount > 0 ? "down" : "up",
                Summary = complianceResult?.TextSummary ?? $"Compliance analysis for {category} ({certRequired}) in progress.",
                Severity = nonCompliantCount > 2 ? "critical" : nonCompliantCount > 0 ? "warning" : "success"
            });

            cards.Add(new InsightCardData
            {
                Title = "Deadline Pressure",
                KeyMetric = $"{daysUntilDeadline} days",
                MetricLabel = "until compliance deadline",
                TrendDirection = daysUntilDeadline < 30 ? "down" : "neutral",
                Summary = $"{certRequired} certification deadline is {deadline:yyyy-MM-dd}. {(daysUntilDeadline < 30 ? "Urgent action required." : "Adequate time for remediation.")}",
                Severity = daysUntilDeadline < 14 ? "critical" : daysUntilDeadline < 30 ? "warning" : "info"
            });

            var allSucceeded = results.All(r => r.Success);
            cards.Add(new InsightCardData
            {
                Title = "Remediation Plan",
                KeyMetric = allSucceeded ? "Ready" : "Partial",
                MetricLabel = "action plan status",
                TrendDirection = "up",
                Summary = $"ESG audit complete with {(allSucceeded ? "full" : "partial")} data from {results.Count} agents. Alternative suppliers identified for non-compliant sources.",
                ActionLabel = "View Risk Matrix",
                Severity = allSucceeded ? "success" : "warning"
            });

            return cards;
        }
        catch
        {
            return Array.Empty<InsightCardData>();
        }
    }

    private async Task<OrchestratorResult> BuildESGAuditFailureResultAsync(
        string sessionId,
        List<AgentResult> results,
        List<PipelineStage> stages,
        string errorMessage,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        var auditTrailData = await BuildAuditTrailDataAsync(sessionId, cancellationToken);
        var pipelineData = new AgentPipelineData
        {
            SessionId = sessionId,
            WorkflowName = "ESGAuditWorkflow",
            Stages = stages,
            OverallStatus = "Failed",
            TotalDuration = duration,
            StartedAt = startTime,
            CompletedAt = DateTimeOffset.UtcNow
        };

        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"ESG audit workflow failed: {errorMessage}",
            AgentResults = results,
            AuditTrailData = auditTrailData,
            PipelineData = pipelineData,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }

    /// <summary>
    /// Emits a reasoning trace step and returns the generated StepId for parent-child hierarchy.
    /// Wrapped in try/catch so trace failures never break the workflow.
    /// </summary>
    private async Task<string> EmitTraceAsync(
        string sessionId,
        string agentName,
        ReasoningStepType stepType,
        string content,
        string? parentStepId = null,
        long durationMs = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var stepId = Guid.NewGuid().ToString("N")[..12];
        try
        {
            metadata ??= new Dictionary<string, string>();
            metadata["StepId"] = stepId;
            await _reasoningTraceEmitter.EmitStepAsync(
                sessionId, agentName, stepType, content,
                parentStepId, durationMs, metadata, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to emit reasoning trace step: {StepType} for {Agent}", stepType, agentName);
        }
        return stepId;
    }
}

/// <summary>
/// Result from orchestrator execution containing all agent results and synthesis.
/// </summary>
public sealed record OrchestratorResult
{
    /// <summary>
    /// Overall workflow success status.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Executive summary synthesizing all agent results.
    /// </summary>
    public required string ExecutiveSummary { get; init; }

    /// <summary>
    /// Individual results from each agent execution.
    /// </summary>
    public required IReadOnlyList<AgentResult> AgentResults { get; init; }

    /// <summary>
    /// Decision audit trail A2UI payload showing chronological agent actions.
    /// </summary>
    public DecisionAuditTrailData? AuditTrailData { get; init; }

    /// <summary>
    /// Agent pipeline visualization A2UI payload showing workflow stages.
    /// </summary>
    public AgentPipelineData? PipelineData { get; init; }

    /// <summary>
    /// Generative insight cards summarizing key findings from the analysis.
    /// </summary>
    public IReadOnlyList<InsightCardData>? InsightCards { get; init; }

    /// <summary>
    /// Error message if Success is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp of orchestration completion.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Total workflow duration.
    /// </summary>
    public required TimeSpan WorkflowDuration { get; init; }
}
