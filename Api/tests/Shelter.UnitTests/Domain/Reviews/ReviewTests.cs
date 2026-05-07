using Shelter.Domain.Common;
using Shelter.Domain.Reviews;

namespace Shelter.UnitTests.Domain.Reviews;

public class ReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ShelterId = Guid.NewGuid();
    private static readonly Guid ReviewerId = Guid.NewGuid();

    private static Review ValidReview(Rating rating = Rating.Good, string? comment = "Lovely") =>
        Review.Create(ShelterId, ReviewerId, rating, comment, Now);

    [Fact]
    public void Create_returns_review_with_invariants_satisfied()
    {
        var review = ValidReview();

        review.Id.Should().NotBe(Guid.Empty);
        review.ShelterId.Should().Be(ShelterId);
        review.ReviewerId.Should().Be(ReviewerId);
        review.Rating.Should().Be(Rating.Good);
        review.Comment.Should().Be("Lovely");
        review.CreatedAt.Should().Be(Now);
        review.UpdatedAt.Should().Be(Now);
        review.Pictures.Should().BeEmpty();
    }

    [Fact]
    public void Create_throws_when_rating_is_undefined()
    {
        var act = () => Review.Create(ShelterId, ReviewerId, (Rating)99, null, Now);

        act.Should().Throw<DomainValidationException>().WithMessage("*rating*");
    }

    [Fact]
    public void Edit_updates_rating_and_comment_and_bumps_timestamp()
    {
        var review = ValidReview();
        var later = Now.AddDays(1);

        review.Edit(Rating.Lacking, "Changed my mind", later);

        review.Rating.Should().Be(Rating.Lacking);
        review.Comment.Should().Be("Changed my mind");
        review.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Edit_throws_when_rating_is_undefined()
    {
        var review = ValidReview();

        var act = () => review.Edit((Rating)99, "anything", Now.AddDays(1));

        act.Should().Throw<DomainValidationException>().WithMessage("*rating*");
    }

    [Fact]
    public void AddPicture_assigns_sort_order_by_insertion()
    {
        var review = ValidReview();

        review.AddPicture(Guid.NewGuid(), null, Now);
        review.AddPicture(Guid.NewGuid(), "second", Now);

        review.Pictures.Should().HaveCount(2);
        review.Pictures[0].SortOrder.Should().Be(0);
        review.Pictures[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public void AddPicture_throws_when_asset_id_is_empty()
    {
        var review = ValidReview();

        var act = () => review.AddPicture(Guid.Empty, null, Now);

        act.Should().Throw<DomainValidationException>().WithMessage("*asset*");
    }

    [Fact]
    public void RemovePicture_is_silent_when_picture_does_not_exist()
    {
        var review = ValidReview();

        var act = () => review.RemovePicture(Guid.NewGuid(), Now.AddHours(1));

        act.Should().NotThrow();
        review.Pictures.Should().BeEmpty();
    }

    [Fact]
    public void WrittenBy_matches_reviewer_id()
    {
        var review = ValidReview();

        review.WrittenBy(ReviewerId).Should().BeTrue();
        review.WrittenBy(Guid.NewGuid()).Should().BeFalse();
    }
}
