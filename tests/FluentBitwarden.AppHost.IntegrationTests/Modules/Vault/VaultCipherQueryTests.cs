using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed class VaultCipherQueryTests
{
    [Fact]
    public void Default_query_excludes_deleted_ciphers_and_include_deleted_returns_them()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var activeCipher = VaultQueryTestData.Login("active-cipher", keySession.UserKey.Key, "Active item");
        var deletedCipher = VaultQueryTestData.Login(
            "deleted-cipher",
            keySession.UserKey.Key,
            "Deleted item",
            deletedDate: new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
        context.SeedCache([activeCipher, deletedCipher]);
        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        Assert.Equal(
            new[] { "active-cipher" },
            vault.GetCiphers(new VaultCipherQuery()).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(
            new[] { "active-cipher", "deleted-cipher" },
            vault.GetCiphers(new VaultCipherQuery { IncludeDeleted = true }).Select(static cipher => cipher.Id.ToString()));
    }

    [Fact]
    public void Query_applies_favorite_folder_and_type_filters_individually_and_together()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        FolderId folderA = FolderId.Parse("folder-a");
        FolderId folderB = FolderId.Parse("folder-b");
        var ciphers = new[]
        {
            VaultQueryTestData.Login("favorite-a", keySession.UserKey.Key, "Favorite A", folderId: folderA),
            VaultQueryTestData.Login("favorite-b", keySession.UserKey.Key, "Favorite B", folderId: folderB),
            VaultQueryTestData.Login("not-favorite", keySession.UserKey.Key, "Not favorite", folderId: folderA, favorite: false),
            VaultQueryTestData.Card("favorite-card", keySession.UserKey.Key, "Card", folderId: folderA),
            VaultQueryTestData.Login(
                "deleted-favorite-a",
                keySession.UserKey.Key,
                "Deleted favorite A",
                folderId: folderA,
                deletedDate: new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero))
        };
        context.SeedCache(ciphers, [
            VaultManagerTestData.Folder("folder-a", "Folder A", keySession.UserKey.Key),
            VaultManagerTestData.Folder("folder-b", "Folder B", keySession.UserKey.Key)
        ]);
        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        Assert.Equal(
            new[] { "favorite-card", "favorite-a", "favorite-b" },
            vault.GetCiphers(new VaultCipherQuery { FavoritesOnly = true }).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(
            new[] { "favorite-card", "favorite-a", "not-favorite" },
            vault.GetCiphers(new VaultCipherQuery { FolderId = folderA }).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(
            new[] { "favorite-card" },
            vault.GetCiphers(new VaultCipherQuery { CipherType = CipherType.Card }).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(
            new[] { "favorite-a" },
            vault.GetCiphers(new VaultCipherQuery
            {
                FavoritesOnly = true,
                FolderId = folderA,
                CipherType = CipherType.Login
            }).Select(static cipher => cipher.Id.ToString()));
    }

    [Fact]
    public void Query_searches_names_usernames_card_fields_and_identity_email_trimmed_and_case_insensitively()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var ciphers = new[]
        {
            VaultQueryTestData.Login("name-match", keySession.UserKey.Key, "NameNeedle in title"),
            VaultQueryTestData.Login("username-match", keySession.UserKey.Key, "Unrelated login", "UserNeedle@example.test"),
            VaultQueryTestData.Card("holder-match", keySession.UserKey.Key, "Unrelated holder", cardholderName: "Cardholder Needle"),
            VaultQueryTestData.Card("brand-match", keySession.UserKey.Key, "Unrelated brand", brand: "Visa"),
            VaultQueryTestData.Identity(
                "identity-match",
                keySession.UserKey.Key,
                "Unrelated identity",
                fieldName: "email",
                fieldValue: "EmailNeedle@example.test"),
            VaultQueryTestData.Identity("null-fields", keySession.UserKey.Key, "Unrelated null identity")
        };
        context.SeedCache(ciphers);
        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        foreach ((string searchText, string expectedId) in new[]
        {
            ("  nAmEnEeDlE  ", "name-match"),
            ("  USERneedle  ", "username-match"),
            ("  hOlDeR nEeDlE  ", "holder-match"),
            ("  vIsA  ", "brand-match"),
            ("  eMaIlNeEdLe  ", "identity-match")
        })
        {
            Assert.Equal(
                new[] { expectedId },
                vault.GetCiphers(new VaultCipherQuery { SearchText = searchText })
                    .Select(static cipher => cipher.Id.ToString()));
        }

        Assert.Empty(vault.GetCiphers(new VaultCipherQuery { SearchText = "missing needle" }));
        Assert.Empty(vault.GetCiphers(new VaultCipherQuery { SearchText = "null-field-needle" }));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("firstName")]
    [InlineData("middleName")]
    [InlineData("lastName")]
    [InlineData("company")]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("username")]
    [InlineData("city")]
    [InlineData("state")]
    [InlineData("country")]
    [InlineData("postalCode")]
    public void Query_searches_each_identity_field_trimmed_case_insensitively(string fieldName)
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        context.SeedCache(
            [
                VaultQueryTestData.Identity(
                    "matching-identity-field",
                    keySession.UserKey.Key,
                    "Neutral identity name",
                    fieldName,
                    "IdentityFieldNeedle"),
                VaultQueryTestData.Identity("neutral-identity", keySession.UserKey.Key, "Another neutral identity")
            ]);
        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        Assert.Equal(
            new[] { "matching-identity-field" },
            vault.GetCiphers(new VaultCipherQuery { SearchText = "  iDeNtItYfIeLdNeEdLe  " })
                .Select(static cipher => cipher.Id.ToString()));
    }

    [Fact]
    public void Query_sorts_by_each_supported_field_in_both_directions()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        var alpha = VaultQueryTestData.Login(
            "alpha",
            keySession.UserKey.Key,
            "Zulu",
            creationDate: new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero),
            revisionDate: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
        var beta = VaultQueryTestData.Login(
            "beta",
            keySession.UserKey.Key,
            "Alpha",
            creationDate: new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero),
            revisionDate: new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero));
        var gamma = VaultQueryTestData.Login(
            "gamma",
            keySession.UserKey.Key,
            "Mike",
            creationDate: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            revisionDate: new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero));
        context.SeedCache([alpha, beta, gamma]);
        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        foreach ((VaultCipherSortField field, VaultCipherSortDirection direction, string[] expectedIds) in new[]
        {
            (VaultCipherSortField.Name, VaultCipherSortDirection.Ascending, new[] { "beta", "gamma", "alpha" }),
            (VaultCipherSortField.Name, VaultCipherSortDirection.Descending, new[] { "alpha", "gamma", "beta" }),
            (VaultCipherSortField.CreationDate, VaultCipherSortDirection.Ascending, new[] { "gamma", "alpha", "beta" }),
            (VaultCipherSortField.CreationDate, VaultCipherSortDirection.Descending, new[] { "beta", "alpha", "gamma" }),
            (VaultCipherSortField.RevisionDate, VaultCipherSortDirection.Ascending, new[] { "alpha", "beta", "gamma" }),
            (VaultCipherSortField.RevisionDate, VaultCipherSortDirection.Descending, new[] { "gamma", "beta", "alpha" })
        })
        {
            Assert.Equal(
                expectedIds,
                vault.GetCiphers(new VaultCipherQuery { SortField = field, SortDirection = direction })
                    .Select(static cipher => cipher.Id.ToString()));
        }
    }

    [Fact]
    public void Query_limit_is_applied_after_sort_and_handles_empty_singleton_zero_and_adjacent_bounds()
    {
        using var context = new VaultManagerTestContext();
        using var keySession = context.CreateKeySession();
        using (var emptyVault = context.VaultService.Open(context.AccountContext, keySession))
        {
            Assert.Empty(emptyVault.GetCiphers(new VaultCipherQuery()));
            Assert.Empty(emptyVault.GetCiphers(new VaultCipherQuery { Limit = 0 }));
        }

        var singleton = VaultQueryTestData.Login("only", keySession.UserKey.Key, "Only item");
        context.SeedCache([singleton]);
        using (var singletonVault = context.VaultService.Open(context.AccountContext, keySession))
        {
            Assert.Empty(singletonVault.GetCiphers(new VaultCipherQuery { Limit = 0 }));
            Assert.Equal("only", Assert.Single(singletonVault.GetCiphers(new VaultCipherQuery { Limit = 1 })).Id.ToString());
            Assert.Equal("only", Assert.Single(singletonVault.GetCiphers(new VaultCipherQuery { Limit = 2 })).Id.ToString());
        }

        var ciphers = new[]
        {
            VaultQueryTestData.Login("zulu", keySession.UserKey.Key, "Zulu"),
            VaultQueryTestData.Login("alpha", keySession.UserKey.Key, "Alpha"),
            VaultQueryTestData.Login("mike", keySession.UserKey.Key, "Mike")
        };
        context.SeedCache(ciphers);
        using var vault = context.VaultService.Open(context.AccountContext, keySession);

        Assert.Empty(vault.GetCiphers(new VaultCipherQuery { Limit = 0 }));
        Assert.Equal(new[] { "alpha" }, vault.GetCiphers(new VaultCipherQuery { Limit = 1 }).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(new[] { "alpha", "mike" }, vault.GetCiphers(new VaultCipherQuery { Limit = 2 }).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(new[] { "alpha", "mike", "zulu" }, vault.GetCiphers(new VaultCipherQuery { Limit = 3 }).Select(static cipher => cipher.Id.ToString()));
        Assert.Equal(new[] { "alpha", "mike", "zulu" }, vault.GetCiphers(new VaultCipherQuery { Limit = 4 }).Select(static cipher => cipher.Id.ToString()));
    }
}
