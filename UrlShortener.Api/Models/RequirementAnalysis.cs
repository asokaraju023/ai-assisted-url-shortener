namespace UrlShortener.Api.Models;

public sealed record RequirementAnalysisRequest(string Requirement);

public sealed record RequirementAnalysisResponse(
    string Requirement,
    string EngineeringProblem,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Ambiguities,
    IReadOnlyList<string> Tasks,
    IReadOnlyList<string> AiAssistance,
    IReadOnlyList<string> ValidationChecks,
    IReadOnlyList<string> Risks);
