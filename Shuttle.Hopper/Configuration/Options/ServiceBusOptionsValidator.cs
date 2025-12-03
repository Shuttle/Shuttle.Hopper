using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ServiceBusOptionsValidator : IValidateOptions<ServiceBusOptions>
{
    public ValidateOptionsResult Validate(string? name, ServiceBusOptions options)
    {
        Guard.AgainstNull(options);

        if (string.IsNullOrWhiteSpace(options.Inbox.WorkTransportUri))
        {
            return ValidateOptionsResult.Fail(string.Format(Resources.RequiredTransportUriMissingException, "Inbox.WorkTransportUri"));
        }

        if (string.IsNullOrWhiteSpace(options.Outbox.WorkTransportUri))
        {
            return ValidateOptionsResult.Fail(string.Format(Resources.RequiredTransportUriMissingException, "Outbox.WorkTransportUri"));
        }

        foreach (var messageRoute in options.MessageRoutes)
        {
            if (!Uri.TryCreate(messageRoute.Uri, UriKind.Absolute, out _))
            {
                return ValidateOptionsResult.Fail(string.Format(Resources.InvalidUriException, messageRoute.Uri, "MessageRoute.Uri"));
            }

            if (!messageRoute.Specifications.Any())
            {
                return ValidateOptionsResult.Fail(Resources.MessageRoutesRequireSpecificationException);
            }
        }

        foreach (var uriMapping in options.UriMappings)
        {
            if (!Uri.TryCreate(uriMapping.SourceUri, UriKind.Absolute, out _))
            {
                return ValidateOptionsResult.Fail(string.Format(Resources.InvalidUriException, uriMapping.SourceUri, nameof(uriMapping.SourceUri)));
            }

            if (!Uri.TryCreate(uriMapping.TargetUri, UriKind.Absolute, out _))
            {
                return ValidateOptionsResult.Fail(string.Format(Resources.InvalidUriException, uriMapping.TargetUri, nameof(uriMapping.TargetUri)));
            }
        }

        return ValidateOptionsResult.Success;
    }
}