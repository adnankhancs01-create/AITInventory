using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ProductCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // because no IDENTITY
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public string Description { get; set; }

}