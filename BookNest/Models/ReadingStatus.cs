using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class ReadingStatus
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ReadingList> ReadingLists { get; set; } = new List<ReadingList>();
}
