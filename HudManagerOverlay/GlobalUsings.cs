// UseWindowsForms=true (for the tray icon) pulls in System.Windows.Forms alongside WPF's
// System.Windows.*, and several type names collide. Resolve them once here instead of
// qualifying every reference across the codebase.
global using UserControl = System.Windows.Controls.UserControl;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using Point = System.Windows.Point;
global using Application = System.Windows.Application;
global using MessageBox = System.Windows.MessageBox;
global using Color = System.Windows.Media.Color;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using FontFamily = System.Windows.Media.FontFamily;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using Button = System.Windows.Controls.Button;
