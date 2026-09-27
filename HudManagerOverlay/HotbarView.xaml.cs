using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HudManagerOverlay;

public sealed record HotbarSlot(string Label, ushort Vk, ushort? Modifier = null);

// Two decks ported from the old app's default keybinds (main.js) — seat (piloting) and foot
// (on-foot) actions are entirely different in Star Citizen, so pressing a seat slot on foot
// does nothing even if the hotbar itself works perfectly (confirmed live 2026-09-27). MainWindow
// auto-switches between them: entering a seat is detected via GameLogWatcher, leaving one via
// holding the exit-seat key (Y) — see MainWindow's ExitSeat* fields, same approach as main.js.
public partial class HotbarView : UserControl
{
    private const ushort VK_MENU = 0x12; // Alt

    public static readonly HotbarSlot?[] SeatDeck =
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

    public static readonly HotbarSlot?[] FootDeck =
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

    private readonly List<Border> _slotBorders = new();
    private HotbarSlot?[] _deck = FootDeck;
    private bool _armed;

    public HotbarView()
    {
        InitializeComponent();
        BuildSlots();
    }

    // Swaps the active deck (seat vs foot) and rebuilds the slot UI in place — called by
    // MainWindow when GameLogWatcher/exit-seat-hold detects a context change.
    public void SetDeck(HotbarSlot?[] deck)
    {
        if (ReferenceEquals(_deck, deck)) return;
        _deck = deck;
        BuildSlots();
        SetArmed(_armed); // rebuilt border lost the armed-state color; reapply it
    }

    private void BuildSlots()
    {
        SlotPanel.Children.Clear();
        _slotBorders.Clear();
        for (int i = 0; i < _deck.Length; i++)
        {
            var slot = _deck[i];
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
        _armed = armed;
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

    public HotbarSlot? GetSlot(int index) => index >= 0 && index < _deck.Length ? _deck[index] : null;
}
