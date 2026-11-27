namespace SharpQuill.Application.ContentPackage.DTOs;

public record ManifestContentCount(
    int Articles,
    int Pages
);

public record ManifestDataCount(
    int Tags,
    int Categories,
    int Settings
);

public record ManifestDto(
    string FormatVersion,
    DateTime ExportedAt,
    string Source,
    ManifestContentCount ContentCount,
    int MediaCount,
    ManifestDataCount? DataCount = null
);
