namespace WNAB.Services;

/// <summary>
/// DTO for creating a new category - no circular references
/// </summary>
public record CategoryCreateDto(string Name);

/// <summary>
/// DTO for returning category data - no User navigation property
/// </summary>
public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    string? Color,
    bool IsIncome,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
