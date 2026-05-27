namespace UmobQuiz.Api.Application.Questions;

/// <summary>
/// Generates comparison questions between two providers by active station count.
/// </summary>
public sealed class ProviderStationComparisonQuestionGenerator : IQuestionGenerator
{
    public string TemplateName => "station_comparison";

    public IEnumerable<GeneratedQuestion> Generate(IReadOnlyList<ProviderStats> stats, Random random)
    {
        for (var i = 0; i < stats.Count; i++)
        {
            for (var j = i + 1; j < stats.Count; j++)
            {
                var left = stats[i];
                var right = stats[j];
                if (left.ActiveStationCount == right.ActiveStationCount)
                {
                    continue;
                }

                var winner = left.ActiveStationCount > right.ActiveStationCount ? left : right;
                var options = new[] { left.DisplayName, right.DisplayName, "They are equal" };
                var correctIndex = winner.DisplayName == left.DisplayName ? 0 : 1;

                yield return new GeneratedQuestion(
                    QuestionGeneratorHelpers.NewQuestionId(TemplateName, $"{left.ProviderKey}-{right.ProviderKey}"),
                    TemplateName,
                    $"Which provider has more active stations: {left.DisplayName} or {right.DisplayName}?",
                    options,
                    correctIndex);
            }
        }
    }
}
