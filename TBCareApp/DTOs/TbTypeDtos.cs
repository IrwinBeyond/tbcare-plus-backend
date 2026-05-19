namespace TBCarePlus.API.DTOs;

public class TbTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BodyArea { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTbTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BodyArea { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateTbTypeDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BodyArea { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}
