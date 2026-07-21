using System.ComponentModel.DataAnnotations;

namespace BookNest.Models;

public class Rating
{
    public int Id { get; set; }

    [Range(1, 5)]
    public int Value { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
