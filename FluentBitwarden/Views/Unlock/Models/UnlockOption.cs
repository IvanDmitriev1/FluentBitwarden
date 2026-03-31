using FluentBitwarden.Modules.Session.Models.Unlock;

namespace FluentBitwarden.Views.Unlock.Models;

public readonly record struct UnlockOption(UnlockMethod Method, string Title);