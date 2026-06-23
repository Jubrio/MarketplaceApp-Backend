namespace MarketplaceApp.Application.DTOs.Category;

public class CategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int? VendorId { get; set; }
    public List<CategoryResponseDto> SubCategories { get; set; } = new();
}