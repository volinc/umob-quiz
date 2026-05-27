using UmobQuiz.Api.Application.Questions;

namespace UmobQuiz.Tests;

public class QuestionGeneratorTests
{
    private static readonly IReadOnlyList<ProviderStats> SampleStats =
    [
        new ProviderStats("citibike", "Citi Bike NYC", 1200, 80),
        new ProviderStats("bicing", "Bicing Barcelona", 600, 40),
        new ProviderStats("baywheels", "Bay Wheels", 900, 55)
    ];

    [Fact]
    public void TotalBikesGenerator_ProducesQuestionsWithValidCorrectIndex()
    {
        var generator = new TotalBikesQuestionGenerator();
        var questions = generator.Generate(SampleStats, new Random(42)).ToList();

        Assert.NotEmpty(questions);
        Assert.All(questions, q =>
        {
            Assert.Equal(q.Options[q.CorrectOptionIndex], ExtractExpectedCount(q.Text, SampleStats));
        });
    }

    [Fact]
    public void BikeComparisonGenerator_ProducesPairwiseQuestions()
    {
        var generator = new ProviderBikeComparisonQuestionGenerator();
        var questions = generator.Generate(SampleStats, new Random(1)).ToList();

        Assert.True(questions.Count >= 3);
        Assert.Contains(questions, q => q.Text.Contains("more active bikes", StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtractExpectedCount(string text, IReadOnlyList<ProviderStats> stats)
    {
        var provider = stats.First(s => text.Contains(s.DisplayName, StringComparison.OrdinalIgnoreCase));
        return provider.ActiveBikeCount.ToString();
    }
}
