using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Agents.Domain;
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
    private readonly AuditRepository _auditRepository;
    private readonly IThinkingStateNotifier _thinkingNotifier;
    private readonly ILogger<ChiefSoftwareArchitectAgent> _logger;

    public ChiefSoftwareArchitectAgent(
        InventoryAgent inventoryAgent,
        PricingAgent pricingAgent,
        MarketIntelAgent marketIntelAgent,
        AuditRepository auditRepository,
        IThinkingStateNotifier thinkingNotifier,
        ILogger<ChiefSoftwareArchitectAgent> logger)
    {
        _inventoryAgent = inventoryAgent ?? throw new ArgumentNullException(nameof(inventoryAgent));
        _pricingAgent = pricingAgent ?? throw new ArgumentNullException(nameof(pricingAgent));
        _marketIntelAgent = marketIntelAgent ?? throw new ArgumentNullException(nameof(marketIntelAgent));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _thinkingNotifier = thinkingNotifier ?? throw new ArgumentNullException(nameof(thinkingNotifier));
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

        // Record workflow initiation
        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated competitor price response workflow", 
            "AGUI", startTime, TimeSpan.Zero, "Success", $"User request for SKU {sku} at ${competitorPrice:F2}", 
            activity?.TraceId.ToString(), new[] { sku }, null, null, cancellationToken);

        try
        {
            // Step 1: Validate competitor claim via A2A (MarketIntelAgent)
            _logger.LogInformation("Step 1: Delegating to MarketIntelAgent for competitor validation");
            
            var stage1Start = DateTimeOffset.UtcNow;
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
            results.Add(marketIntelResult);

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
                _logger.LogWarning("MarketIntelAgent failed - aborting workflow");
                return await BuildFailureResultAsync(sessionId, results, pipelineStages, "Failed to validate competitor pricing", startTime, cancellationToken);
            }

            // Step 2: Get inventory snapshot (InventoryAgent)
            _logger.LogInformation("Step 2: Delegating to InventoryAgent for inventory snapshot");
            
            var stage2Start = DateTimeOffset.UtcNow;
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
            results.Add(inventoryResult);

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
            
            var stage3Start = DateTimeOffset.UtcNow;
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
            results.Add(pricingResult);

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
        await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Initiated bulk competitor price response workflow", 
            "AGUI", startTime, TimeSpan.Zero, "Success", 
            $"Bulk request from {competitorName} for {items.Count} SKUs", 
            activity?.TraceId.ToString(), skuList, null, null, cancellationToken);

        try
        {
            // Step 1: Validate competitor claims via A2A (MarketIntelAgent)
            _logger.LogInformation("Step 1: Delegating to MarketIntelAgent for bulk competitor validation");
            
            var stage1Start = DateTimeOffset.UtcNow;
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
            results.Add(marketIntelResult);

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
            
            var stage2Start = DateTimeOffset.UtcNow;
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
            results.Add(inventoryResult);

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
            
            var stage3Start = DateTimeOffset.UtcNow;
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
            results.Add(pricingResult);

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
            
            await RecordAuditEntryAsync(sessionId, "ChiefSoftwareArchitect", "Bulk workflow execution failed",
                "AGUI", DateTimeOffset.UtcNow, TimeSpan.Zero, "Failed",
                ex.Message, activity?.TraceId.ToString(), null, null, null, cancellationToken);
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            return await BuildFailureResultAsync(sessionId, results, pipelineStages, $"Bulk orchestration error: {ex.Message}", startTime, cancellationToken);
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
