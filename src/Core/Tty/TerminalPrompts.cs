using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace GitCredentialManager.Tty;

/// <summary>
/// Factory for creating various common interactive prompts.
/// </summary>
public static class TerminalPrompts
{
    /// <summary>
    /// Create a prompt for a line of text.
    /// </summary>
    public static TextPrompt<string> CreateText(string label) =>
        new TextPrompt<string>(label).AllowEmpty();

    /// <summary>
    /// Create a prompt for a secret (masked) line of text.
    /// </summary>
    public static TextPrompt<string> CreateSecret(string label, char? mask = null) =>
        new TextPrompt<string>(label).AllowEmpty().Secret(mask);

    public static SelectionPrompt<SelectionPromptItem<T>> CreateSelection<T>() =>
        new SelectionPrompt<SelectionPromptItem<T>>()
            .UseConverter(x => x.Label)
            .AddCancelResult(() => throw new OperationCanceledException("User cancelled the prompt"));

    extension<T>(IPrompt<T> prompt)
    {
        public T Show(IConsoleService console) =>
            console.ShowPrompt(prompt);

        public Task<T> ShowAsync(IConsoleService console, CancellationToken ct = default) =>
            console.ShowPromptAsync(prompt, ct);
    }

    extension<T> (SelectionPrompt<SelectionPromptItem<T>> prompt)
    {
        public ISelectionItem<SelectionPromptItem<T>> AddChoice(string label, T item) =>
            prompt.AddChoice(new SelectionPromptItem<T>(label, item));

        public SelectionPrompt<SelectionPromptItem<T>> AddChoices(IEnumerable<T> items, Func<T, string> labelFunc)
        {
            foreach (var item in items)
            {
                prompt.AddChoice(new SelectionPromptItem<T>(labelFunc(item), item));
            }

            return prompt;
        }

        public SelectionPrompt<SelectionPromptItem<T>> AddChoices(params (string Label, T Item)[] items)
        {
            foreach (var (label, item) in items)
            {
                prompt.AddChoice(new SelectionPromptItem<T>(label, item));
            }

            return prompt;
        }

        public async Task<T> ShowAsync(IConsoleService console, CancellationToken ct = default)
        {
            SelectionPromptItem<T> choice = await console.ShowPromptAsync(prompt, ct);
            return choice.Item;
        }
    }
}

public class SelectionPromptItem<T>(string label, T item)
{
    public string Label { get; } = label;
    public T Item { get; } = item;
}
