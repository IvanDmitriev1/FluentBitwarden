using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Settings.Models;

public readonly record struct ThemeOption(ElementTheme Value, string Title)
{
    public static readonly ThemeOption[] Options =
        [Create(ElementTheme.Default), Create(ElementTheme.Light), Create(ElementTheme.Dark)];

    public static ThemeOption Create(ElementTheme value) => value switch
    {
        ElementTheme.Default => new ThemeOption(value, "System"),
        ElementTheme.Light => new ThemeOption(ElementTheme.Light, "Light"),
        ElementTheme.Dark => new ThemeOption(ElementTheme.Dark, "Dark"),
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
};