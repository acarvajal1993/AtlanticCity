using BulkLoad.Application.DTOs;
using BulkLoad.Application.Interfaces;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace BulkLoad.Infrastructure.Services;

public class ExcelReaderService : IExcelReaderService
{
    private readonly ILogger<ExcelReaderService> _logger;

    public ExcelReaderService(ILogger<ExcelReaderService> logger)
    {
        _logger = logger;
    }

    public Task<List<ExcelRowDto>> ReadExcelAsync(Stream fileStream)
    {
        var rows = new List<ExcelRowDto>();

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheet(1);
            var usedRange = worksheet.RangeUsed();

            if (usedRange is null)
            {
                _logger.LogWarning("El archivo Excel está vacío");
                return Task.FromResult(rows);
            }

            var firstRow = usedRange.FirstRow().RowNumber();
            var lastRow = usedRange.LastRow().RowNumber();

            // Mapear columnas por nombre de encabezado
            var columnMap = MapearColumnas(worksheet, firstRow);

            _logger.LogInformation("Leyendo filas {FirstRow} a {LastRow}", firstRow + 1, lastRow);

            // Leer datos (saltando encabezado)
            for (int rowNum = firstRow + 1; rowNum <= lastRow; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                
                var excelRow = new ExcelRowDto
                {
                    NumeroFila = rowNum,
                    CodigoProducto = GetCellValue(row, columnMap, "codigoproducto", "codigo"),
                    Descripcion = GetCellValue(row, columnMap, "descripcion", "nombre"),
                    Cantidad = GetCellDecimalValue(row, columnMap, "cantidad", "qty"),
                    PrecioUnitario = GetCellDecimalValue(row, columnMap, "preciounitario", "precio"),
                    Categoria = GetCellValue(row, columnMap, "categoria", "category"),
                    Periodo = GetCellValue(row, columnMap, "periodo", "period")
                };

                // Validar fila
                ValidarFila(excelRow);

                rows.Add(excelRow);
            }

            _logger.LogInformation("Se leyeron {Count} filas del Excel", rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer archivo Excel");
            throw;
        }

        return Task.FromResult(rows);
    }

    private static Dictionary<string, int> MapearColumnas(IXLWorksheet worksheet, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var row = worksheet.Row(headerRow);

        foreach (var cell in row.CellsUsed())
        {
            var headerName = cell.GetString().Trim().ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "");
            
            if (!string.IsNullOrEmpty(headerName))
            {
                map[headerName] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    private static string GetCellValue(IXLRow row, Dictionary<string, int> columnMap, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (columnMap.TryGetValue(name, out int colNum))
            {
                return row.Cell(colNum).GetString().Trim();
            }
        }
        return string.Empty;
    }

    private static decimal GetCellDecimalValue(IXLRow row, Dictionary<string, int> columnMap, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (columnMap.TryGetValue(name, out int colNum))
            {
                var cell = row.Cell(colNum);
                if (cell.TryGetValue(out decimal value))
                {
                    return value;
                }
                
                // Intentar parsear como string
                var strValue = cell.GetString().Trim();
                if (decimal.TryParse(strValue, out decimal parsed))
                {
                    return parsed;
                }
            }
        }
        return 0;
    }

    private static void ValidarFila(ExcelRowDto row)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(row.CodigoProducto))
        {
            // No es error crítico si toda la fila está vacía
            if (!string.IsNullOrWhiteSpace(row.Descripcion) || row.Cantidad > 0 || row.PrecioUnitario > 0)
            {
                errores.Add("Código de producto vacío");
            }
        }

        if (row.Cantidad < 0)
        {
            errores.Add("Cantidad negativa");
        }

        if (row.PrecioUnitario < 0)
        {
            errores.Add("Precio negativo");
        }

        if (errores.Count > 0)
        {
            row.EsValido = false;
            row.MensajeError = string.Join("; ", errores);
        }
    }
}
