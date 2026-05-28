using BitwardenApi.Models;
using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.Contracts.Session.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountIUnlockMethod
{
    UnlockMethodType UnlockMethod { get; }
}