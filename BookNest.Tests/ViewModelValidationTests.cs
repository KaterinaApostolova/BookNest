using System.ComponentModel.DataAnnotations;
using BookNest.Models;
using BookNest.ViewModels;

namespace BookNest.Tests;

public class ViewModelValidationTests
{
    private static bool Validate(object model, out List<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, results, true);
    }

    [Test]
    public void ReviewViewModel_RequiresRatingBetweenOneAndFive()
    {
        var model = new ReviewViewModel { BookId = 1, Rating = 6, Content = "Great book!" };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ReviewViewModel.Rating))), Is.True);
    }

    [Test]
    public void ReviewViewModel_IsValid_WithRatingInRangeAndContent()
    {
        var model = new ReviewViewModel { BookId = 1, Rating = 4, Content = "Loved it." };

        var isValid = Validate(model, out _);

        Assert.That(isValid, Is.True);
    }

    [Test]
    public void ReviewViewModel_DefaultsToNotAnonymous()
    {
        var model = new ReviewViewModel();

        Assert.That(model.IsAnonymous, Is.False);
    }

    [Test]
    public void ForumTopicViewModel_RequiresTitleAndContent()
    {
        var model = new ForumTopicViewModel { ForumId = 1, Title = string.Empty, Content = string.Empty };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ForumTopicViewModel.Title))), Is.True);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ForumTopicViewModel.Content))), Is.True);
    }

    [Test]
    public void ForumTopicViewModel_TitleCannotExceedMaxLength()
    {
        var model = new ForumTopicViewModel
        {
            ForumId = 1,
            Title = new string('a', 151),
            Content = "Valid content"
        };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ForumTopicViewModel.Title))), Is.True);
    }

    [Test]
    public void ForumPostViewModel_RequiresContent()
    {
        var model = new ForumPostViewModel { ForumTopicId = 1, Content = string.Empty };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ForumPostViewModel.Content))), Is.True);
    }

    [Test]
    public void ResetPasswordViewModel_RequiresMatchingPasswords()
    {
        var model = new ResetPasswordViewModel
        {
            UserId = "user-1",
            Token = "token-value",
            Password = "NewPass123!",
            ConfirmPassword = "Different123!"
        };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ResetPasswordViewModel.ConfirmPassword))), Is.True);
    }

    [Test]
    public void ResetPasswordViewModel_IsValid_WithMatchingPasswordsOfSufficientLength()
    {
        var model = new ResetPasswordViewModel
        {
            UserId = "user-1",
            Token = "token-value",
            Password = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        var isValid = Validate(model, out _);

        Assert.That(isValid, Is.True);
    }

    [Test]
    public void ResetPasswordViewModel_RejectsPasswordShorterThanMinimum()
    {
        var model = new ResetPasswordViewModel
        {
            UserId = "user-1",
            Token = "token-value",
            Password = "123",
            ConfirmPassword = "123"
        };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ResetPasswordViewModel.Password))), Is.True);
    }

    [Test]
    public void ContributionRequestViewModel_RequiresMessage()
    {
        var model = new ContributionRequestViewModel
        {
            RequestType = ContributionRequestTypes.GeneralSuggestion,
            Message = string.Empty
        };

        var isValid = Validate(model, out var results);

        Assert.That(isValid, Is.False);
        Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ContributionRequestViewModel.Message))), Is.True);
    }

    [Test]
    public void AuthorDisplayViewModel_DefaultsToBookNestMemberFallback()
    {
        var model = new AuthorDisplayViewModel();

        Assert.That(model.FallbackLabel, Is.EqualTo("BookNest member"));
        Assert.That(model.AnonymousLabel, Is.EqualTo("Anonymous"));
        Assert.That(model.IsAnonymous, Is.False);
    }

    [Test]
    public void ReactionSummary_DefaultsToZeroCountsAndNoReaction()
    {
        var summary = new ReactionSummary();

        Assert.That(summary.Likes, Is.EqualTo(0));
        Assert.That(summary.Dislikes, Is.EqualTo(0));
        Assert.That(summary.MyReaction, Is.Null);
    }
}
