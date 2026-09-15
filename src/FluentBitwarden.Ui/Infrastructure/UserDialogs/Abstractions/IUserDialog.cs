using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.Infrastructure.UserDialogs.Abstractions;

public interface IUserDialog<TResult>
{
    bool TryGetResult([MaybeNullWhen(false)] out TResult result);
}
