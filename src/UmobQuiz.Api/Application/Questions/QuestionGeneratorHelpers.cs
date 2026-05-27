namespace UmobQuiz.Api.Application.Questions;

internal static class QuestionGeneratorHelpers
{
    public static IReadOnlyList<string> BuildNumericOptions(int correctValue, Random random, int optionCount = 4)
    {
        var options = new HashSet<int> { correctValue };
        while (options.Count < optionCount)
        {
            var delta = random.Next(5, Math.Max(50, correctValue / 4 + 10));
            var candidate = random.Next(2) == 0 ? correctValue - delta : correctValue + delta;
            if (candidate >= 0)
            {
                options.Add(candidate);
            }
        }

        return options.OrderBy(_ => random.Next()).Select(x => x.ToString()).ToList();
    }

    public static int IndexOfCorrect(IReadOnlyList<string> options, int correctValue) =>
        options.ToList().FindIndex(o => o == correctValue.ToString());

    public static string NewQuestionId(string template, string seed) =>
        $"{template}:{seed}:{Guid.NewGuid():N}";
}
