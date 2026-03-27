using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public string Description { get; set; }

    // Foreign Key
    public int CategoryId { get; set; }

    // Navigation Property
    public ProductCategory Category { get; set; }
}