using FluentValidation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Tv
{
    public interface IAddSeriesValidator : IValidator<Series> { }

    public class AddSeriesValidator : AbstractValidator<Series>, IAddSeriesValidator
    {
        public AddSeriesValidator(RootFolderValidator rootFolderValidator,
                                  SeriesPathValidator seriesPathValidator,
                                  SeriesAncestorValidator seriesAncestorValidator,
                                  SeriesTitleSlugValidator seriesTitleSlugValidator)
        {
            RuleFor(c => c.Path).Cascade(CascadeMode.StopOnFirstFailure)
                                .IsValidPath()
                                .SetValidator(rootFolderValidator)
                                .SetValidator(seriesPathValidator)
                                .SetValidator(seriesAncestorValidator);

            RuleFor(c => c.TitleSlug).SetValidator(seriesTitleSlugValidator);
        }
    }
}
