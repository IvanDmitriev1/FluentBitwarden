using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Sessions;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock;
using FluentBitwarden.Contracts.Modules.Accounts.Unlock.WindowsHello;
using System.Text.Json;

namespace FluentBitwarden.CommandPalette.Pages;

internal sealed partial class UnlockVaultPage : ContentPage
{
    public const string PageId = "unlock-vault";
    private readonly UnlockFormContent _unlockFormContent;

    public UnlockVaultPage(UnlockFormContent unlockFormContent)
    {
        _unlockFormContent = unlockFormContent;

        Id = PageId;
        Name = "Unlock";
        Title = "Unlock FluentBitwarden";
        Icon = Icons.Unlock;
    }

    public override IContent[] GetContent()
    {
        return [_unlockFormContent];
    }

    internal sealed partial class UnlockFormContent : FormContent
    {
        private const string ActionPropertyName = "Action";
        private const string AccountUserIdPropertyName = "AccountUserId";
        private const string MasterPasswordPropertyName = "MasterPassword";
        private const string MasterPasswordUnlockAction = "MasterPasswordUnlock";
        private const string WindowsHelloUnlockAction = "WindowsHelloUnlock";

        private readonly IAccountsClient _accountsClient;
        private readonly ISessionClient _sessionClient;
        private readonly IWindowsHelloUnlockClient _windowsHelloUnlockClient;

        public UnlockFormContent(
            IAccountsClient accountsClient,
            ISessionClient sessionClient,
            IWindowsHelloUnlockClient windowsHelloUnlockClient)
        {
            _accountsClient = accountsClient;
            _sessionClient = sessionClient;
            _windowsHelloUnlockClient = windowsHelloUnlockClient;

            TemplateJson = BuildCurrentTemplateJson();
        }

        public override ICommandResult SubmitForm(string inputs, string data)
        {
            try
            {
                using var inputDocument = ParseJsonOrEmpty(inputs);
                using var dataDocument = ParseJsonOrEmpty(data);

                JsonElement inputRoot = inputDocument.RootElement;
                JsonElement dataRoot = dataDocument.RootElement;

                string action = dataRoot.GetStringProperty(ActionPropertyName);
                string accountUserId = dataRoot.GetStringProperty(AccountUserIdPropertyName);
                if (string.IsNullOrWhiteSpace(accountUserId))
                    return CommandResult.ShowToast("Selected account is unavailable.");

                AccountProfile? selectedAccount = ResolveAccount(accountUserId);
                if (selectedAccount is null)
                    return CommandResult.ShowToast("Selected account is unavailable.");

                return action switch
                {
                    MasterPasswordUnlockAction => UnlockWithMasterPassword(inputRoot, selectedAccount),
                    WindowsHelloUnlockAction => UnlockWithWindowsHello(selectedAccount),
                    _ => CommandResult.ShowToast("Unknown unlock action."),
                };
            }
            catch (JsonException)
            {
                return CommandResult.ShowToast("Unlock form data was invalid.");
            }
            catch (OperationCanceledException)
            {
                return CommandResult.ShowToast("Unlock canceled.");
            }
            catch (Exception ex)
            {
                return CommandResult.ShowToast($"Unlock failed: {ex.Message}");
            }
        }

        private string BuildCurrentTemplateJson()
        {
            AccountProfile[] accounts = _accountsClient
                .GetAccountsAsync(CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (accounts.Length == 0)
                return BuildNoAccountsTemplateJson();

            AccountProfile selectedAccount = accounts[0];
            WindowsHelloStatus status = _windowsHelloUnlockClient
                .GetStatusAsync(new GetWindowsHelloStatusRequest(selectedAccount.UserId), CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return BuildTemplateJson(selectedAccount, status is { IsSupported: true, IsEnabled: true });
        }

        private static string BuildTemplateJson(AccountProfile selectedAccount, bool windowsHelloAvailable)
        {
            string accountEmailJson = ToJsonStringLiteral(selectedAccount.Email);
            string accountUserIdJson = ToJsonStringLiteral(selectedAccount.UserId.ToString());
            string descriptionJson = ToJsonStringLiteral(BuildDescription(windowsHelloAvailable));

            string windowsHelloActionJson = windowsHelloAvailable
                ? $$"""
                    ,
                    {
                      "type": "Action.Submit",
                      "title": "Unlock with Windows Hello",
                      "associatedInputs": "none",
                      "data": {
                        "Action": "WindowsHelloUnlock",
                        "AccountUserId": {{accountUserIdJson}}
                      }
                    }
                    """
                : string.Empty;

            return $$"""
                     {
                       "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                       "type": "AdaptiveCard",
                       "version": "1.6",
                       "body": [
                         {
                           "type": "TextBlock",
                           "text": "Unlock Bitwarden vault",
                           "size": "Medium",
                           "weight": "Bolder",
                           "wrap": true
                         },
                         {
                           "type": "TextBlock",
                           "text": {{accountEmailJson}},
                           "wrap": true
                         },
                         {
                           "type": "TextBlock",
                           "text": {{descriptionJson}},
                           "wrap": true,
                           "isSubtle": true
                         },
                         {
                           "type": "Input.Text",
                           "id": "MasterPassword",
                           "label": "Master password",
                           "style": "password",
                           "isRequired": true,
                           "errorMessage": "Master password is required.",
                           "isMultiline": false,
                           "placeholder": "Enter master password"
                         }
                       ],
                       "actions": [
                         {
                           "type": "Action.Submit",
                           "title": "Unlock",
                           "data": {
                             "Action": "MasterPasswordUnlock",
                             "AccountUserId": {{accountUserIdJson}}
                           }
                         }
                         {{windowsHelloActionJson}}
                       ]
                     }
                     """;
        }

        private static string BuildNoAccountsTemplateJson()
        {
            return """
                   {
                     "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                     "type": "AdaptiveCard",
                     "version": "1.6",
                     "body": [
                       {
                         "type": "TextBlock",
                         "text": "No accounts found",
                         "size": "Medium",
                         "weight": "Bolder",
                         "wrap": true
                       },
                       {
                         "type": "TextBlock",
                         "text": "Sign in to FluentBitwarden before unlocking your vault from Command Palette.",
                         "wrap": true,
                         "isSubtle": true
                       }
                     ]
                   }
                   """;
        }

        private static string BuildDescription(bool windowsHelloAvailable) =>
            windowsHelloAvailable
                ? "Use your master password or Windows Hello to unlock this account."
                : "Use your master password to unlock this account.";

        private AccountProfile? ResolveAccount(string accountUserId)
        {
            AccountProfile[] accounts = _accountsClient
                .GetAccountsAsync(CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return accounts.FirstOrDefault(account =>
                StringComparer.OrdinalIgnoreCase.Equals(account.UserId.ToString(), accountUserId));
        }

        private ICommandResult UnlockWithWindowsHello(AccountProfile selectedAccount)
        {
            using var ownerWindow = new HiddenWindow("FluentBitwarden_ComPlateExt_Wnd");
            return UnlockVault(new AccountUnlockRequest.WindowsHelloRequest(selectedAccount, ownerWindow.Hwnd));
        }

        private ICommandResult UnlockWithMasterPassword(JsonElement input, AccountProfile selectedAccount)
        {
            string masterPassword = input.TryGetProperty(MasterPasswordPropertyName, out JsonElement masterPasswordElement)
                ? masterPasswordElement.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(masterPassword))
                return CommandResult.ShowToast("Enter your master password.");

            return UnlockVault(new AccountUnlockRequest.MasterPasswordRequest(selectedAccount, masterPassword));
        }

        private ICommandResult UnlockVault(AccountUnlockRequest request)
        {
            AccountUnlockOutcome outcome = _sessionClient
                .UnlockAsync(request, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return outcome switch
            {
                AccountUnlockOutcome.Success => CommandResult.GoBack(),
                AccountUnlockOutcome.WindowsHelloCancelled => CommandResult.ShowToast("Unlock canceled."),
                AccountUnlockOutcome.RequiresOnlineReauth => CommandResult.ShowToast("Sign in again to unlock this account."),
                AccountUnlockOutcome.Failure failure => CommandResult.ShowToast(failure.Reason),
                _ => CommandResult.ShowToast("Unlock failed.")
            };
        }

        private static JsonDocument ParseJsonOrEmpty(string json) =>
            JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);

        private static string ToJsonStringLiteral(string value) =>
            $"\"{JsonEncodedText.Encode(value)}\"";
    }
}
