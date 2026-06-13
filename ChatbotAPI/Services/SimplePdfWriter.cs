using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ChatbotAPI.Services
{
    /// <summary>
    /// Utilitario simples para transformar um markdown resumido em PDF.
    /// </summary>
    public static class SimplePdfWriter
    {
        /// <summary>
        /// Monta o documento final com cabecalho, conteudo e rodape.
        /// </summary>
        public static byte[] BuildSummaryPdf(string title, string subtitle, string markdownContent, string footerText)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var cleanMarkdown = markdownContent ?? string.Empty;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(style => style.FontFamily("Helvetica").FontSize(11));

                    page.Header().Column(header =>
                    {
                        header.Item().Text(title).FontSize(22).SemiBold().FontColor("#0F172A");
                        header.Item().PaddingTop(4).Text(subtitle).FontSize(10).FontColor("#475569");
                        header.Item().PaddingTop(12).LineHorizontal(1).LineColor("#CBD5E1");
                    });

                    page.Content().PaddingTop(18).Column(column =>
                    {
                        foreach (var block in ParseMarkdownBlocks(cleanMarkdown))
                        {
                            switch (block.Kind)
                            {
                                case MarkdownBlockKind.Title:
                                    column.Item().Text(block.Text).FontSize(16).SemiBold().FontColor("#1D4ED8");
                                    break;
                                case MarkdownBlockKind.Subtitle:
                                    column.Item().Text(block.Text).FontSize(13).SemiBold().FontColor("#2563EB");
                                    break;
                                case MarkdownBlockKind.Bullet:
                                    column.Item().PaddingLeft(10).Text($"- {block.Text}").FontSize(11).FontColor("#334155");
                                    break;
                                case MarkdownBlockKind.Paragraph:
                                default:
                                    column.Item().Text(block.Text).FontSize(11).FontColor("#0F172A");
                                    break;
                            }

                            column.Item().PaddingBottom(4);
                        }
                    });

                    page.Footer().AlignRight().Text(footerText).FontSize(9).FontColor("#64748B");
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Quebra o markdown em blocos simples para renderizar titulo, subtitulo, lista e paragrafo.
        /// </summary>
        private static List<MarkdownBlock> ParseMarkdownBlocks(string markdown)
        {
            var blocks = new List<MarkdownBlock>();

            foreach (var rawLine in markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("# "))
                {
                    blocks.Add(new MarkdownBlock(MarkdownBlockKind.Title, line[2..].Trim()));
                    continue;
                }

                if (line.StartsWith("## "))
                {
                    blocks.Add(new MarkdownBlock(MarkdownBlockKind.Subtitle, line[3..].Trim()));
                    continue;
                }

                if (line.StartsWith("- "))
                {
                    blocks.Add(new MarkdownBlock(MarkdownBlockKind.Bullet, line[2..].Trim()));
                    continue;
                }

                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, line));
            }

            return blocks;
        }

        private enum MarkdownBlockKind
        {
            Title,
            Subtitle,
            Bullet,
            Paragraph
        }

        private readonly record struct MarkdownBlock(MarkdownBlockKind Kind, string Text);
    }
}
