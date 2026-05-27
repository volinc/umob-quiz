namespace UmobQuiz.Api.Application.Questions;

/// <summary>
/// Generates questions about total active station counts for a single provider.
/// </summary>
public sealed class TotalStationsQuestionGenerator : IQuestionGenerator
{
    public string TemplateName => "total_stations";

    public IEnumerable<GeneratedQuestion> Generate(IReadOnlyList<ProviderStats> stats, Random random)
    {
        foreach (var provider in stats.Where(s => s.ActiveStationCount > 0))
        {
            var correct = provider.ActiveStationCount;
            var options = QuestionGeneratorHelpers.BuildNumericOptions(correct, random);
            var correctIndex = QuestionGeneratorHelpers.IndexOfCorrect(options, correct);

            yield return new GeneratedQuestion(
                QuestionGeneratorHelpers.NewQuestionId(TemplateName, provider.ProviderKey),
                TemplateName,
                $"How many active stations does {provider.DisplayName} currently have?",
                options,
                correctIndex);
        }
    }
}
