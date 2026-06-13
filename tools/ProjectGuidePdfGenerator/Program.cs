using ChatbotAPI.Services;

var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

var inputPath = args.Length > 0
    ? Path.GetFullPath(args[0], Directory.GetCurrentDirectory())
    : Path.Combine(projectRoot, "Guia_Aula_Projeto.md");

var outputPath = args.Length > 1
    ? Path.GetFullPath(args[1], Directory.GetCurrentDirectory())
    : Path.Combine(projectRoot, "Guia_Aula_Projeto.pdf");

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Arquivo de entrada nao encontrado: {inputPath}");
    return 1;
}

var markdown = await File.ReadAllTextAsync(inputPath);

var pdfBytes = SimplePdfWriter.BuildSummaryPdf(
    title: "Guia Completo do Projeto ChatbotAPI",
    subtitle: $"Material de aula gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
    markdownContent: markdown,
    footerText: "Projeto ChatbotAPI"
);

var outputDirectory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrWhiteSpace(outputDirectory))
{
    Directory.CreateDirectory(outputDirectory);
}

await File.WriteAllBytesAsync(outputPath, pdfBytes);

Console.WriteLine(outputPath);
return 0;
