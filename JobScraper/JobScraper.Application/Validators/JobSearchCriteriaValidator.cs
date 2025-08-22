using FluentValidation;
using JobScraper.Domain.DTOs;

namespace JobScraper.Application.Validators;

public class JobSearchCriteriaValidator : AbstractValidator<JobSearchCriteria>
{
    public JobSearchCriteriaValidator()
    {
        RuleFor(x => x.Titles)
            .NotEmpty()
            .WithMessage("At least one job title must be specified");

        RuleFor(x => x.MaxPerSite)
            .GreaterThan(0)
            .When(x => x.MaxPerSite.HasValue)
            .WithMessage("Max per site must be greater than 0");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(DateTime.Now)
            .When(x => x.DateFrom.HasValue)
            .WithMessage("Date from cannot be in the future");
    }
}
