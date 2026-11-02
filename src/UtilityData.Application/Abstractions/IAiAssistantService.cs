namespace UtilityData.Application.Abstractions;
public sealed record AiAnalysisRequest(string Question, string Context);
public sealed record AiAnalysisResponse(string Answer, bool UsedLocalModel);
public interface IAiAssistantService
{
    Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken);
}
