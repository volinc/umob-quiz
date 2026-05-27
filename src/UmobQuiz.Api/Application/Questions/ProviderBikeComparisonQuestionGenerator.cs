namespace UmobQuiz.Api.Application.Questions;

/// <summary>
/// Generates comparison questions between two providers by active bike count.
/// </summary>
public sealed class ProviderBikeComparisonQuestionGenerator : IQuestionGenerator
{
    public string TemplateName => "bike_comparison";

    public IEnumerable<GeneratedQuestion> Generate(IReadOnlyList<ProviderStats> stats, Random random)
    {
        for (var i = 0; i < stats.Count; i++)
        {
            for (var j = i + 1; j < stats.Count; j++)
            {
                var left = stats[i];
                var right = stats[j];
                if (left.ActiveBikeCount == right.ActiveBikeCount)
                {
                    continue;
                }

                var winner = left.ActiveBikeCount > right.ActiveBikeCount ? left : right;
                var options = new[] { left.DisplayName, right.DisplayName, "They are equal" };
                var correctIndex = left.ActiveBikeCount == right.ActiveBikeCount
                    ? 2
                    : winner.DisplayName == left.DisplayName ? 0 : 1;

                yield return new GeneratedQuestion(
                    QuestionGeneratorHelpers.NewQuestionId(TemplateName, $"{left.ProviderKey}-{right.ProviderKey}"),
                    TemplateName,
                    $"Which provider has more active bikes: {left.DisplayName} or {right.DisplayName}?",
                    options,
                    correctIndex);
            }
        }
    }
}
