namespace UmobQuiz.Api.Application.Questions;

public sealed record ProviderStats(
    string ProviderKey,
    string DisplayName,
    int ActiveBikeCount,
    int ActiveStationCount);

public sealed record QuestionPoolSnapshot(IReadOnlyList<GeneratedQuestion> Questions);

public sealed record GeneratedQuestion(
    string Id,
    string TemplateName,
    string Text,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex);
