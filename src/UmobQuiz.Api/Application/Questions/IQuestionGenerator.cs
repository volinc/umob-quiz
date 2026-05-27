namespace UmobQuiz.Api.Application.Questions;

public interface IQuestionGenerator
{
    string TemplateName { get; }
    IEnumerable<GeneratedQuestion> Generate(IReadOnlyList<ProviderStats> stats, Random random);
}
