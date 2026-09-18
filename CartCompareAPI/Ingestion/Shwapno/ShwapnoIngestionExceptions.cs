using System;

namespace CartCompareAPI.Ingestion.Shwapno;

public sealed class UnsupportedShwapnoCategoryException(string message)
    : Exception(message);

public sealed class InvalidShwapnoSourceDataException(string message)
    : Exception(message);
