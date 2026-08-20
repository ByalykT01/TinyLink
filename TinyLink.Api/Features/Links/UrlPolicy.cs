using FluentValidation;

namespace TinyLink.Api.Features.Links;

public sealed record UrlInput(string? Raw)
{
    public Uri? Parsed { get; } = Uri.TryCreate(Raw, UriKind.Absolute, out var uri) ? uri : null;
}

public sealed class UrlPolicy : AbstractValidator<UrlInput>
{
    public const int MaxLength = 2000;

    public UrlPolicy()
    {
        RuleFor(x => x.Raw)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("URL is required.")
            .MaximumLength(MaxLength).WithMessage($"Must be at most {MaxLength} characters in length.");

        RuleFor(x => x.Parsed)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Must be an absolute URL.")
            .Must(uri => uri!.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                .WithMessage("Must use the http or https scheme.")
            .Must(uri => string.IsNullOrEmpty(uri!.UserInfo))
                .WithMessage("Must not bear embedded credentials.")
            .Must(uri => uri!.AbsoluteUri.Length <= MaxLength)
                .WithMessage("Must not exceed the maximum length.")
            .When(x => x.Raw is { Length: > 0 } and { Length: <= MaxLength });
    }
}
