using FluentBitwarden.Application.Models;

namespace FluentBitwarden.Application.Abstractions;

internal interface IAppSessionResolver
{
    Task<AppSessionResolution> ResolveAsync();
}