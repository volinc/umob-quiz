namespace UmobQuiz.Client.Services;

public sealed record DownloadedFile(byte[] Content, string FileName, string ContentType);
