namespace CartCompareAPI.Canonicalization.Packaging;

public interface IPackageTypeParser
{
    ParsedPackageType? Parse(string productName);
}
