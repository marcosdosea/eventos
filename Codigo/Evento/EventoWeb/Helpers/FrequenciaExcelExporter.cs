using ClosedXML.Excel;
using System.Globalization;

namespace EventoWeb.Helpers
{
    /// <summary>
    /// Gerador de planilha eletrônica formatada em Excel (.xlsx) usando ClosedXML
    /// para emissão de relatório gerencial de frequência compatível com homologação no SIGAA.
    /// </summary>
    public static class FrequenciaExcelExporter
    {
        public static readonly string[] Cabecalhos =
        {
            "CPF",
            "Nome Completo",
            "E-mail",
            "Curso / Subevento",
            "Carga Horária Total (Horas)",
            "Total Horas Presentes",
            "Percentual de Frequência (%)",
            "Situação da Frequência",
            "Histórico de Entradas e Saídas"
        };

        public static byte[] Gerar(string? nomeEvento, string? nomeSubEvento, IEnumerable<FrequenciaRelatorioItem> itens)
        {
            ArgumentNullException.ThrowIfNull(itens);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Frequência");

            // Linha 1: Título formatado em destaque
            var titulo = FrequenciaCsvExporter.Titulo(nomeEvento, nomeSubEvento);
            worksheet.Cell(1, 1).Value = titulo;
            worksheet.Range(1, 1, 1, Cabecalhos.Length).Merge();
            worksheet.Row(1).Height = 28;

            var estiloTitulo = worksheet.Cell(1, 1).Style;
            estiloTitulo.Font.Bold = true;
            estiloTitulo.Font.FontSize = 13;
            estiloTitulo.Font.FontColor = XLColor.White;
            estiloTitulo.Fill.BackgroundColor = XLColor.FromArgb(13, 110, 253); // Azul Bootstrap
            estiloTitulo.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            estiloTitulo.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Linha 2: Vazia (separação visual)
            worksheet.Row(2).Height = 10;

            // Linha 3: Cabeçalhos das Colunas
            worksheet.Row(3).Height = 24;
            for (int col = 0; col < Cabecalhos.Length; col++)
            {
                var cell = worksheet.Cell(3, col + 1);
                cell.Value = Cabecalhos[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(33, 37, 41); // Cinza grafite escuro
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(108, 117, 125);
            }

            // Linhas de dados (a partir da Linha 4)
            int row = 4;
            foreach (var item in itens)
            {
                worksheet.Row(row).Height = 20;

                worksheet.Cell(row, 1).SetValue(item.Cpf ?? string.Empty);
                worksheet.Cell(row, 2).SetValue(item.NomeCompleto ?? string.Empty);
                worksheet.Cell(row, 3).SetValue(item.Email ?? string.Empty);
                worksheet.Cell(row, 4).SetValue(item.CursoOuSubevento ?? string.Empty);

                // Carga Horária Total (numérico)
                worksheet.Cell(row, 5).SetValue((double)item.CargaHorariaTotal);
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Total Horas Presentes (numérico)
                worksheet.Cell(row, 6).SetValue((double)item.TotalHorasPresentes);
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Percentual de Frequência (porcentagem formatada)
                worksheet.Cell(row, 7).SetValue((double)(item.PercentualFrequencia / 100m));
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "0.00%";
                worksheet.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Situação da Frequência com formatação condicional de cores
                var cellSituacao = worksheet.Cell(row, 8);
                cellSituacao.SetValue(item.SituacaoFrequencia);
                cellSituacao.Style.Font.Bold = true;
                cellSituacao.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellSituacao.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if (string.Equals(item.SituacaoFrequencia, "APTO PARA CERTIFICADO", StringComparison.OrdinalIgnoreCase))
                {
                    // Verde suave com texto verde escuro
                    cellSituacao.Style.Fill.BackgroundColor = XLColor.FromArgb(212, 237, 218);
                    cellSituacao.Style.Font.FontColor = XLColor.FromArgb(21, 87, 36);
                }
                else
                {
                    // Vermelho suave com texto vermelho escuro
                    cellSituacao.Style.Fill.BackgroundColor = XLColor.FromArgb(248, 215, 218);
                    cellSituacao.Style.Font.FontColor = XLColor.FromArgb(114, 28, 36);
                }

                // Histórico de Entradas e Saídas
                worksheet.Cell(row, 9).SetValue(item.HistoricoEntradasSaidas ?? string.Empty);
                worksheet.Cell(row, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Bordas suaves em todas as células da linha
                for (int col = 1; col <= Cabecalhos.Length; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(row, col).Style.Border.OutsideBorderColor = XLColor.FromArgb(222, 226, 230);
                }

                row++;
            }

            // Ajusta largura automática das colunas
            worksheet.Columns().AdjustToContents();

            // Limita a largura da coluna de histórico e habilita quebra de texto
            worksheet.Column(9).Style.Alignment.WrapText = true;
            if (worksheet.Column(9).Width > 60)
            {
                worksheet.Column(9).Width = 60;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public static string NomeArquivo(
            string? nomeEvento,
            uint idEvento,
            string? nomeSubEvento,
            uint? idSubEvento,
            DateTime geradoEm)
        {
            var partes = new List<string>
            {
                "frequencia",
                CsvFormat.Slug(nomeEvento, "evento"),
                idEvento.ToString(CultureInfo.InvariantCulture)
            };

            if (idSubEvento.HasValue && idSubEvento.Value > 0)
            {
                partes.Add(CsvFormat.Slug(nomeSubEvento, "subevento"));
                partes.Add(idSubEvento.Value.ToString(CultureInfo.InvariantCulture));
            }

            partes.Add(geradoEm.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture));
            return string.Join("-", partes) + ".xlsx";
        }
    }
}
