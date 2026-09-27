using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HudManagerOverlay;

public sealed record HotbarSlot(string Label, ushort Vk, ushort? Modifier = null);

// Two decks ported from the old app's default keybinds (main.js) — seat (piloting) and foot
// (on-foot) actions are entirely different in Star Citizen, so pressing a seat slot on foot
// does nothing even if the hotbar itself works perfectly (confirmed live 2026-09-27). Real
// auto-switching between them needs Game.log parsing to know if you're seated — not built yet,
// so this defaults to the foot deck since that's what's actually testable right now.
public partial class HotbarView : UserControl
{
    private const ushort VK_MENU = 0x12; // Alt

    private static readonly HotbarSlot?[] SeatDeck =
    {
        new("Mining", 0x4D),      // M
        new("Scan", 0x56),        // V
        new("Shields", 0x75),     // F6
        new("Gear", 0x4E),        // N
        new("Nav Lights", 0x4C),  // L
        new("Quantum", 0x42),     // B
        new("VTOL", 0x4B),        // K
        new("Decouple", 0x43),    // C
        null,
    };

    private static readonly HotbarSlot?[] FootDeck =
    {
        new("Helmet", 0x48, VK_MENU),   // Alt+H
        new("Visor", 0x50, VK_MENU),    // Alt+P
        new("Holster", 0x52),           // R
        new("Flashlight", 0x54),        // T
        new("Inner Thought", 0x46),     // F
        new("Medpen", 0x43),            // C
        new("Scan/Ping", 0x09),         // Tab
        new("mobiGlas", 0x70),          // F1
        null,                            // Suicide is a hold-action, not a simple tap — skipped
    };

    private static readonly HotbarSlot?[] DefaultDeck = FootDeck;

    private readonly List<Border> _slotBorders = new();

    public HotbarView()
    {
        InitializeComponent();
        for (int i = 0; i < DefaultDeck.Length; i++)
        {
            var slot = DefaultDeck[i];
            var border = new Border
            {
                Width = 56, Height = 56, Margin = new Thickness(3),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(0x40, 0x1A, 0x25, 0x30)),
                BorderBrush = (Brush)Application.Current.Resources["TextDim"],
                BorderThickness = new Thickness(1),
            };
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text = (i + 1).ToString(), FontFamily = new FontFamily("Consolas"), FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextDim"], HorizontalAlignment = HorizontalAlignment.Center,
            });
            stack.Children.Add(new TextBlock
            {
                Text = slot?.Label ?? "—", FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
            });
            border.Child = stack;
            _slotBorders.Add(border);
            SlotPanel.Children.Add(border);
        }
    }

    public void SetArmed(bool armed)
    {
        OuterBorder.BorderBrush = (Brush)Application.Current.Resources[armed ? "Amber" : "Cyan"];
    }

    public void FlashSlot(int index)
    {
        if (index < 0 || index >= _slotBorders.Count) return;
        _slotBorders[index].Background = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xB8, 0x40));
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(150) };
        timer.Tick += (_, _) =>
        {
            _slotBorders[index].Background = new SolidColorBrush(Color.FromArgb(0x40, 0x1A, 0x25, 0x30));
            timer.Stop();
        };
        timer.Start();
    }

    public HotbarSlot? GetSlot(int index) => index >= 0 && index < DefaultDeck.Length ? DefaultDeck[index] : null;
}
