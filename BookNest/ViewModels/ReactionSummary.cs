namespace BookNest.ViewModels;

public class ReactionSummary
{
    public int Likes { get; set; }
    public int Dislikes { get; set; }

    /// <summary>true = current user liked it, false = disliked, null = no reaction from them.</summary>
    public bool? MyReaction { get; set; }
}
