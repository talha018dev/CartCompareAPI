using System;
using System.Text.Json;
using CartCompareApi.Ingestion.Shwapno.Entities;

namespace CartCompareAPI.Ingestion.Shwapno.Import;

public sealed class ShwapnoJsonReader
{
    public async static Task<List<ShwapnoProduct>> ReadProductsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The Shwapno dairy data file was not found.", filePath);

        await using var stream = File.OpenRead(filePath);
        var sourceProducts = await JsonSerializer.DeserializeAsync<List<ShwapnoProduct>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
            ?? throw new InvalidDataException("The Shwapno dairy data file does not contain a product array.");

        return sourceProducts;
    }
}
