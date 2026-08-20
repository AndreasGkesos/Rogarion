using ColorCode;
using ColorCode.Common;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Rogarion.Core.Models;
using Windows.ApplicationModel.DataTransfer;

namespace Rogarion.App.Controls;

public sealed partial class ChatMessageControl : UserControl
{
    private static readonly RichTextBlockFormatter Formatter = new();

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(ChatMessage),
            typeof(ChatMessageControl),
            new PropertyMetadata(null, OnMessageChanged));

    public ChatMessage? Message
    {
        get => (ChatMessage?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ChatMessageControl()
    {
        InitializeComponent();
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ChatMessageControl)d;

        if (e.OldValue is ChatMessage oldMessage)
        {
            oldMessage.PropertyChanged -= control.OnMessagePropertyChanged;
        }

        if (e.NewValue is ChatMessage newMessage)
        {
            newMessage.PropertyChanged += control.OnMessagePropertyChanged;
        }

        control.Render();
    }

    private void OnMessagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatMessage.Segments))
        {
            Render();
        }
    }

    private void Render()
    {
        RoleTextBlock.Text = Message?.Role switch
        {
            ChatRole.User => "You",
            ChatRole.Assistant => "Assistant",
            ChatRole.System => "System",
            _ => string.Empty
        };

        var isUser = Message?.Role == ChatRole.User;
        BubbleBorder.HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        BubbleBorder.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[
            isUser ? "AccentFillColorDefaultBrush" : "CardBackgroundFillColorDefaultBrush"];
        RoleRow.HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        RoleTextBlock.Foreground = isUser
            ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextOnAccentFillColorSecondaryBrush"]
            : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

        if (isUser && !string.IsNullOrEmpty(Message?.ModeName))
        {
            ModeBadge.Visibility = Visibility.Visible;
            ModeBadgeText.Text = Message.ModeName;
            ModeBadge.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerOnAcrylicFillColorDefaultBrush"];
            ModeBadgeText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextOnAccentFillColorSecondaryBrush"];
        }
        else
        {
            ModeBadge.Visibility = Visibility.Collapsed;
        }

        SegmentsPanel.Children.Clear();

        if (Message is null)
        {
            return;
        }

        foreach (var segment in Message.Segments)
        {
            SegmentsPanel.Children.Add(segment.IsCode
                ? BuildCodeBlock(segment)
                : BuildProseBlock(segment, isUser));
        }
    }

    private static TextBlock BuildProseBlock(MessageSegment segment, bool isUser)
    {
        var textBlock = new TextBlock
        {
            Text = segment.Text,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };

        if (isUser)
        {
            textBlock.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];
        }

        return textBlock;
    }

    private static FrameworkElement BuildCodeBlock(MessageSegment segment)
    {
        var richTextBlock = new RichTextBlock
        {
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono, Consolas, monospace"),
            FontSize = 13,
            IsTextSelectionEnabled = true
        };

        var language = ResolveLanguage(segment.Language);
        if (language is not null)
        {
            Formatter.FormatRichTextBlock(segment.Text, language, richTextBlock);
        }
        else
        {
            richTextBlock.Blocks.Add(new Paragraph
            {
                Inlines = { new Run { Text = segment.Text } }
            });
        }

        var copyButton = new Button
        {
            Content = "Copy",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(8, 2, 8, 2)
        };
        copyButton.Click += (_, _) =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(segment.Text);
            Clipboard.SetContent(dataPackage);
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock
        {
            Text = segment.Language ?? "code",
            Opacity = 0.6,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(copyButton, 1);
        header.Children.Add(copyButton);

        var container = new StackPanel { Spacing = 4 };
        container.Children.Add(header);

        var codeBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10),
            Child = richTextBlock
        };
        container.Children.Add(codeBorder);

        return container;
    }

    private static ILanguage? ResolveLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        return Languages.FindById(language) ?? language.ToLowerInvariant() switch
        {
            "cs" or "csharp" => Languages.CSharp,
            "js" or "javascript" => Languages.JavaScript,
            "ts" or "typescript" => Languages.Typescript,
            "py" or "python" => Languages.Python,
            "xml" => Languages.Xml,
            "html" => Languages.Html,
            "css" => Languages.Css,
            "sql" => Languages.Sql,
            "powershell" or "ps1" => Languages.PowerShell,
            "cpp" or "c++" => Languages.Cpp,
            "java" => Languages.Java,
            "php" => Languages.Php,
            "fsharp" or "f#" => Languages.FSharp,
            _ => null
        };
    }
}
