namespace UmobQuiz.Api.Application.Questions;

/// <summary>
/// Generates questions about total active bike counts for a single provider.
/// </summary>
public sealed class TotalBikesQuestionGenerator : IQuestionGenerator
{
    public string TemplateName => "total_bikes";

    public IEnumerable<GeneratedQuestion> Generate(IReadOnlyList<ProviderStats> stats, Random random)
    {
        foreach (var provider in stats.Where(s => s.ActiveBikeCount > 0))
        {
            var correct = provider.ActiveBikeCount;
            var options = QuestionGeneratorHelpers.BuildNumericOptions(correct, random);
            var correctIndex = QuestionGeneratorHelpers.IndexOfCorrect(options, correct);

            yield return new GeneratedQuestion(
                QuestionGeneratorHelpers.NewQuestionId(TemplateName, provider.ProviderKey),
                TemplateName,
                $"How many bikes are currently available across {provider.DisplayName} (free-floating + docked)?",
                options,
                correctIndex);
        }
    }
}
