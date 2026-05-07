using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Session.Abstractions;

internal interface IAccountIUnlockMethod
{
    UnlockMethodType UnlockMethod { get; }
}