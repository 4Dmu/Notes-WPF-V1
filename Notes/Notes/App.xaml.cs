using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using Shell.Core.Extensions;
using Shell.MVVM.Commands;
using Shell.WPF.Core.Markup;
using Shell.WPF.NavShell.Controls;
using Shell.WPF.UI.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Notes
{
    public partial class App : Application, INotifyPropertyChanged
    {
        #region Feilds
        private IHost _host;

        private const string appName = "notepadNX"; 
        #endregion

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Properties
        private bool showStatusBar = true;
        public bool ShowStatusBar
        {
            get => showStatusBar;
            set => MarkupExtensions.SetProperty(ref showStatusBar, value, OnPropertyChanged);
        }

        private bool isWordWraped;
        public bool IsWordWrapped
        {
            get => isWordWraped;
            set => MarkupExtensions.SetProperty(ref isWordWraped, value, OnPropertyChanged);
        }
        private double scale = 1.5;
        public double Scale
        {
            get => scale;
            set => MarkupExtensions.SetProperty(ref scale, value, OnPropertyChanged);
        }
        private double max = 10;

        private bool showFind;
        public bool ShowFind
        {
            get => showFind;
            set => MarkupExtensions.SetProperty(ref showFind, value, OnPropertyChanged);
        }

        private bool showFindAndReplace;
        public bool ShowFindAndReplace
        {
            get => showFindAndReplace;
            set { MarkupExtensions.SetProperty(ref showFindAndReplace, value, OnPropertyChanged); ShowFind = value ? value : ShowFind; }
        }

        private bool showSettings;
        public bool ShowSettings
        {
            get => showSettings;
            set => MarkupExtensions.SetProperty(ref showSettings, value, OnPropertyChanged);
        }

        private FontFamily fontFamily = Fonts.SystemFontFamilies.FirstOrDefault(x => x.ToString() == "Segoe UI");

        public FontFamily FontFamily
        {
            get => fontFamily;
            set => MarkupExtensions.SetProperty(ref fontFamily, value, OnPropertyChanged);
        }

        private double fontSize = 12;

        public double FontSize
        {
            get => fontSize;
            set => MarkupExtensions.SetProperty(ref fontSize, value, OnPropertyChanged);
        }

        private FontWeight fontWeight = FontWeights.Normal;

        public FontWeight FontWeight
        {
            get => fontWeight;
            set => MarkupExtensions.SetProperty(ref fontWeight, value, OnPropertyChanged);
        }

        public string File { get; set; } 
        #endregion

        #region Controls
        public RichTextBox txtBox { get; set; }
        private TextBlock settingsTextBlock;
        private PackIconControl searchIcon;
        private PackIconControl clearTextIcon;
        private PackIconControl nextItemIcon;
        private PackIconControl previousItemIcon;
        private PackIconControl optionsIcon;
        private Menu optionsMenu;
        private MenuItem optionsMenuIconItem;
        private PackIconControl closeIcon;
        private PackIconControl expandMoreIcon;
        private PackIconControl settingsIcon;
        private Border findAndReplaceControl;
        private Grid findAndReplaceSpacer1;
        private Grid findAndReplaceSpacer2;
        private Grid findAndReplaceSpacer2b;
        private TextBox findBox;
        private TextBox replaceBox;
        private Grid windowContent;
        private Grid bottomBarContent;
        private Grid lowerTitleBarContent;
        private Grid settingsControl;
        private Menu menu;
        private MenuItem fileMenuItem;
        private MenuItem editMenuItem;
        private MenuItem viewMenuItem;
        private MenuItem zoomSubMenuItem;
        private CheckBox darkThemeMenuChechBar;
        private CheckBox statusBarThemeMenuChechBar;
        private Button replaceButton;
        private Button replaceAllButton;
        private TextBlock fontSettingsHeaderText;
        private PackIconControl fontSettingsHeaderIcon;
        private StackPanel fontSettingsHeader;
        private ComboBox fontSettingsFamilyComboBox;
        private ComboBox fontSettingsWeightComboBox;
        private ComboBox fontSettingsSizeComboBox;
        private Grid fontSettingFamilyContainer;
        private Grid fontSettingWeightContainer;
        private Grid fontSettingSizeContainer;
        private StackPanel fontSettingsContent;
        private Expander fontSettingsControl;
        #endregion

        #region standardized Property Values
        Thickness iconMargin = new Thickness(10, 5, 10, 5);
        double iconHeight = 13;
        double iconWidth = 13;
        private CornerRadius cornerRadius = new CornerRadius(5);
        #endregion

        #region Find and replace
        private List<string> foundMatches { get; set; }
        private List<TextRange> foundMatchesRanges { get; set; }
        private bool matchCase;
        private bool matchWholeWord;
        private int currentMatch;
        #endregion

        #region Configuration
        private IConfiguration? _configuration;
        private bool _useDarkMode;
        private bool _showStatusBar;
        #endregion

        #region Config file paths
        private static FileInfo _path1 = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string _path1a = _path1.Directory + "\\config.json";
        private static string _path2 = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $@"\{appName}\config.json"; 
        #endregion

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) => {

                    

                    if (System.IO.File.Exists(_path1a))
                        builder.AddJsonFile("config.json", optional: true);
                    else if (System.IO.File.Exists(_path2))
                        builder.AddJsonFile(_path2);
                    
                })
                .ConfigureServices(s =>
                {
                    s.AutoRegisterDependencies(this.GetType().Assembly.GetTypes());

                })
                .Build();

            _configuration = _host.Services.GetService<IConfiguration>();

            if (_configuration!= null)
            {
                var settings = _configuration.GetSection("Settings");
                var f = Fonts.SystemFontFamilies.FirstOrDefault(x => x.ToString() == settings.GetValue<string>("FontFamily"));
                if (f != null)
                    FontFamily = f;
                FontSize = settings.GetValue<int>("FontSize");
                FontWeight = settings.GetValue<FontWeight>("FontWeight");
                _useDarkMode = settings.GetValue<bool>("UseDarkMode");
                _showStatusBar = settings.GetValue<bool>("ShowStatusBar");
            }

            ShowStatusBar = _showStatusBar;

            #region Window Content
            txtBox = new RichTextBox() { BorderThickness = new Thickness(0) }.AttachPreviewMouseWheel((s, e) =>
            {
                var d = e.Timestamp;

                if (s is RichTextBox t)
                {
                    if (Keyboard.Modifiers != ModifierKeys.Control)
                        return;

                    if (e.Delta < 0 && Scale > 0.7)
                    {
                        Scale -= 0.1;
                        t.LayoutTransform = new ScaleTransform(Scale, Scale);
                    }
                    else if (e.Delta > 0 && Scale < max)
                    {
                        Scale += 0.1;
                        t.LayoutTransform = new ScaleTransform(Scale, Scale);
                    }

                    //use the start selection TextPointer

                    t.CaretPosition.GetOffsetToPosition(t.Document.ContentStart);

                    //position from offset

                }
            })
                    .SetResourceReferenceAndReturnSelf(RichTextBox.BackgroundProperty, Shell.WPF.UI.Brushes.Layer0BackgroundBrush)
                    .SetResourceReferenceAndReturnSelf(RichTextBox.ForegroundProperty, Shell.WPF.UI.Brushes.ForegroundBrush)
                    .SetValueAndReturnSelf(FlowDocument.LineHeightProperty, 5.0)
                    .SetValueAndReturnSelf(RichTextBox.LayoutTransformProperty, new ScaleTransform(Scale, Scale))
                    .RunMethodAndReturnSelf((s) => s.GotFocus += OnTxtBoxGotFocus)
                    .RunMethodAndReturnSelf((s) => s.SelectionChanged += OnTxtBoxSelectionChanged)
                    .SetBindingAndReturnSelf(RichTextBox.FontFamilyProperty, MarkupExtensions.CreateBinding(nameof(FontFamily), this))
                    .SetBindingAndReturnSelf(RichTextBox.FontSizeProperty, MarkupExtensions.CreateBinding(nameof(FontSize), this))
                    .SetBindingAndReturnSelf(RichTextBox.FontWeightProperty, MarkupExtensions.CreateBinding(nameof(FontWeight), this));
            #endregion

            #region Find and Replace
            searchIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.Search }.SetVAlignment(VerticalAlignment.Center).SetSize(iconWidth - 3, iconHeight - 3)
                   .SetMargin(iconMargin).SetValueAndReturnSelf(Grid.ColumnProperty, 1).SetHAlignment(HorizontalAlignment.Right).SetBackground(Brushes.Transparent)
                   .AssignMouseLeftButtonDown((s, e) => FindText());

            nextItemIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.ArrowDownward }.SetVAlignment(VerticalAlignment.Center)
                .SetSize(iconWidth, iconHeight).SetMargin(iconMargin).SetValueAndReturnSelf(Grid.ColumnProperty, 2).SetBackground(Brushes.Transparent)
                .AssignMouseLeftButtonDown((s, e) => GoToNextMatch());

            clearTextIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.Close }.SetVAlignment(VerticalAlignment.Center).SetSize(iconWidth - 3, iconHeight - 3)
                .SetMargin(new Thickness(0, 5, 40, 5)).SetValueAndReturnSelf(Grid.ColumnProperty, 1).SetHAlignment(HorizontalAlignment.Right).SetBackground(Brushes.Transparent)
                .AssignMouseLeftButtonDown((s, e) => findBox.Text = "");

            previousItemIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.ArrowUpward }.SetVAlignment(VerticalAlignment.Center).SetSize(iconWidth, iconHeight)
                .SetMargin(iconMargin).SetValueAndReturnSelf(Grid.ColumnProperty, 3).SetBackground(Brushes.Transparent)
                .AssignMouseLeftButtonDown((s, e) => GoToPreviousMatch());

            closeIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.Close }.SetVAlignment(VerticalAlignment.Center).SetSize(iconWidth - 3, iconHeight - 3)
                .SetMargin(iconMargin).SetValueAndReturnSelf(Grid.ColumnProperty, 5).SetBackground(Brushes.Transparent).AssignMouseLeftButtonDown((x, e) =>
            {
                ShowFind = false;
                ShowFindAndReplace = false;
            });

            findBox = new TextBox() { MaxLength = 35 }.RunMethodAndReturnSelf((s) => CornerRadiusExtension.SetCornerRadius(s, cornerRadius)).SetSize(height: 30)
                .SetValueAndReturnSelf(Grid.ColumnProperty, 1).RunMethodAndReturnSelf((s) => WatermarkExtension.SetWatermark(s, "Find"));

            replaceBox = new TextBox().RunMethodAndReturnSelf((s) => CornerRadiusExtension.SetCornerRadius(s, cornerRadius))
                .SetValueAndReturnSelf(Grid.ColumnProperty, 0).RunMethodAndReturnSelf((s) => WatermarkExtension.SetWatermark(s, "Replace"))
                .SetMargin(34, 5, 5, 5);

            replaceButton = new Button() { Content = "Replace" }.SetMargin(5).SetValueAndReturnSelf(Grid.ColumnProperty, 1)
                .RunMethodAndReturnSelf((s) => CornerRadiusExtension.SetCornerRadius(s, cornerRadius))
                .SetClick((s, e) => { FindText(); ReplaceMatch(); });
            replaceAllButton = new Button() { Content = "Replace all" }.SetMargin(5).SetValueAndReturnSelf(Grid.ColumnProperty, 2)
                .RunMethodAndReturnSelf((s) => CornerRadiusExtension.SetCornerRadius(s, cornerRadius))
                .SetClick((s, e) => ReplaceAllMatches());

            expandMoreIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.ExpandMore }.SetVAlignment(VerticalAlignment.Center).SetSize(iconWidth, iconHeight).SetMargin(iconMargin)
                .SetBindingAndReturnSelf(PackIconControl.KindProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(ShowFindAndReplace)), this, new BooleanToIconKindConverter()))
                .AssignMouseLeftButtonDown((s, e) => ShowFindAndReplace = !ShowFindAndReplace).SetBackground(Brushes.Transparent);

            optionsIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.Tune }.SetSize(iconWidth, iconHeight)
                .SetBackground(Brushes.Transparent).SetResourceReferenceAndReturnSelf(PackIconControl.ForegroundProperty, Shell.WPF.UI.Brushes.ForegroundBrush);

            optionsMenuIconItem = new MenuItem { Header = optionsIcon, Padding = new Thickness(0) }
                .SetValueAndReturnSelf(MenuItem.StyleProperty, Resources["menuItem"] as Style)
                .SetBackground(Brushes.Transparent).AddSubItems(new CheckBox { Content = "Match Case" }.AssignCheckedHandler((s, e) => matchCase = !matchCase),
                    new CheckBox { Content = "Match Whole Word" }.AssignCheckedHandler((s, e) => matchWholeWord = !matchWholeWord));

            optionsMenu = MarkupExtensions.CreateMenu(new List<UIElement> { optionsMenuIconItem }).SetValueAndReturnSelf(Grid.ColumnProperty, 4)
                .SetSize(width: 14).SetValueAndReturnSelf(Menu.PaddingProperty, new Thickness(0)).SetVAlignment(VerticalAlignment.Center)
                .SetValueAndReturnSelf(Menu.StyleProperty, Resources["mainMenuStyle"] as Style).SetBackground(Brushes.Transparent).SetMargin(5, 0, 5, 0);

            findAndReplaceSpacer2 = new Grid().AddGridRowsAndCols(new RowDefinition[] { }, new[]
            {
                new ColumnDefinition() { Width = GridLength.Auto}, new ColumnDefinition() , new ColumnDefinition(){ Width = GridLength.Auto},
                new ColumnDefinition(){ Width = GridLength.Auto}, new ColumnDefinition() { Width = GridLength.Auto},new ColumnDefinition() { Width = GridLength.Auto}
            })
                .SetBindingAndReturnSelf(Grid.RowSpanProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(ShowFindAndReplace)), this, new BooleanToRowSpanConverter()))
                .AddChildrenToGrid(expandMoreIcon, findBox, clearTextIcon, searchIcon, nextItemIcon, previousItemIcon, optionsMenu, closeIcon);

            findAndReplaceSpacer2b = new Grid().AddGridRowsAndCols(new RowDefinition[] { }, new ColumnDefinition[]
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
            })
                .AddChildrenToGrid(replaceBox, replaceButton, replaceAllButton)
                .SetSize(height: 40)
                .SetValueAndReturnSelf(Grid.RowProperty, 1)
                .SetBindingAndReturnSelf(Grid.VisibilityProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(ShowFindAndReplace)), this, new BooleanToVisibilityConverter()));

            findAndReplaceSpacer1 = new Grid().AddGridRowsAndCols(MarkupExtensions.FillWithType<RowDefinition>(2), new ColumnDefinition[] { })
                .AddChildrenToGrid(findAndReplaceSpacer2, findAndReplaceSpacer2b);

            findAndReplaceControl = new Border()
                .SetResourceReferenceAndReturnSelf(Border.BackgroundProperty, Shell.WPF.UI.Brushes.Layer0BackgroundBrush).SetSize(450, 40)
                .SetMargin(5).SetHAlignment(HorizontalAlignment.Center)
                .SetVAlignment(VerticalAlignment.Top).SetCornerRadius(cornerRadius).BorderThickness(new Thickness(.8))
                .SetResourceReferenceAndReturnSelf(Border.BorderBrushProperty, Shell.WPF.UI.Brushes.ForegroundBrush)
                .SetBindingAndReturnSelf(Border.VisibilityProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(ShowFind)), this, new BooleanToVisibilityConverter()))
                .SetBindingAndReturnSelf(Border.HeightProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(ShowFindAndReplace)), this, new BooleanToHeightConverter()))
                .SetProperty("Child", findAndReplaceSpacer1); 
            #endregion

            #region Settings
            settingsTextBlock = new TextBlock { Text = "Settings", FontWeight = FontWeights.SemiBold, FontSize = 40 }.SetMargin(20);

            fontSettingsHeaderText = new TextBlock { Text = "Font", FontWeight = FontWeights.SemiBold, FontSize = 20 }.SetMargin(5, 0, 5, 0);

            fontSettingsHeaderIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.TextFormat }.SetVAlignment(VerticalAlignment.Center);

            fontSettingsHeader = new StackPanel() { Orientation = Orientation.Horizontal }.SetChildren(fontSettingsHeaderIcon, fontSettingsHeaderText);

            fontSettingsFamilyComboBox = new ComboBox().SetValueAndReturnSelf(Grid.ColumnProperty, 1)
                .SetBindingAndReturnSelf(ComboBox.SelectedItemProperty, MarkupExtensions.CreateBinding(nameof(FontFamily), this))
                .SetValueAndReturnSelf(ComboBox.ItemsSourceProperty, Fonts.SystemFontFamilies);

            fontSettingsWeightComboBox = new ComboBox().SetValueAndReturnSelf(Grid.ColumnProperty, 1)
                .SetValueAndReturnSelf(ComboBox.ItemsSourceProperty, 
                new FontWeight[] {FontWeights.Regular, FontWeights.Bold, FontWeights.SemiBold, 
                    FontWeights.Medium, FontWeights.Thin, FontWeights.Heavy })
                .SetBindingAndReturnSelf(ComboBox.SelectedItemProperty, MarkupExtensions.CreateBinding(nameof(FontWeight), this));

            fontSettingsSizeComboBox = new ComboBox().SetValueAndReturnSelf(Grid.ColumnProperty, 1)
                .SetValueAndReturnSelf(ComboBox.ItemsSourceProperty, TempExtensions.GetNumCollection(8.0, 72.0))
                .SetBindingAndReturnSelf(ComboBox.SelectedItemProperty, MarkupExtensions.CreateBinding(nameof(FontSize), this));

            fontSettingFamilyContainer = new Grid().SetColumnDefinitions(MarkupExtensions.FillWithType<ColumnDefinition>(2))
                .AddChildrenToGrid(new TextBlock { Text = "Family" }, fontSettingsFamilyComboBox);

            fontSettingWeightContainer = new Grid().SetColumnDefinitions(MarkupExtensions.FillWithType<ColumnDefinition>(2))
                .AddChildrenToGrid(new TextBlock { Text = "Weight" }, fontSettingsWeightComboBox);

            fontSettingSizeContainer = new Grid().SetColumnDefinitions(MarkupExtensions.FillWithType<ColumnDefinition>(2))
                .AddChildrenToGrid(new TextBlock { Text = "Size" }, fontSettingsSizeComboBox);

            fontSettingsContent = new StackPanel().SetChildren(fontSettingFamilyContainer, fontSettingWeightContainer, fontSettingSizeContainer);

            fontSettingsControl = new Expander().SetValueAndReturnSelf(Expander.HeaderProperty, fontSettingsHeader)
                .SetValueAndReturnSelf(Grid.RowProperty, 1).SetMargin(20).SetContent(fontSettingsContent);

            settingsControl = new Grid().SetResourceReferenceAndReturnSelf(Grid.BackgroundProperty, Shell.WPF.UI.Brushes.Layer0BackgroundBrush)
                .SetBindingAndReturnSelf(Grid.VisibilityProperty, MarkupExtensions.CreateBinding(nameof(ShowSettings), this, new BooleanToVisibilityConverter()))
                .SetRowDefinitions(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) }, new RowDefinition())
                .SetColumnDefinitions(new ColumnDefinition(), new ColumnDefinition() { Width = new GridLength(.5, GridUnitType.Star) })
                .AddChildrenToGrid(settingsTextBlock, fontSettingsControl);

            settingsIcon = new PackIconControl { Kind = PackIconMaterialDesignKind.Settings, HorizontalAlignment = HorizontalAlignment.Right }
               .SetMargin(10)
               .SetValueAndReturnSelf(Grid.ColumnProperty, 1)
               .AssignMouseLeftButtonDown((s, e) =>
               {
                   ShowSettings = !ShowSettings;
                   NavigationShell.Current.LowerTitleBarVisibility = Visibility.Collapsed;
                   NavigationShell.Current.BackIconVisibility = BarIconVisibility.Top;
                   NavigationShell.Current.OverrideBackButton = true;
                   bool status = ShowStatusBar;
                   ShowStatusBar = false;
                   NavigationShell.Current.BackButtonCommand = new Shell.MVVM.Commands.AsyncRelayCommand(async () =>
                   {
                       ShowSettings = !ShowSettings;
                       NavigationShell.Current.LowerTitleBarVisibility = Visibility.Visible;
                       ShowStatusBar = status;
                       NavigationShell.Current.BackIconVisibility = BarIconVisibility.Hidden;
                   }, () => true);
               })
               .SetBackground(Brushes.Transparent);

            #endregion

            #region Menu
            statusBarThemeMenuChechBar = new CheckBox { Content = "Status Bar", IsChecked = _showStatusBar }
                    .AssignCheckedHandler((s, e) => ShowStatusBar = !ShowStatusBar);

            darkThemeMenuChechBar = new CheckBox { Content = "Dark Theme", IsChecked = _useDarkMode }
                .AssignCheckedHandler((s, e) =>
                {
                    NavigationShell.Current.ToggleTheme();
                    NavigationShell.Current.Icon = NavigationShell.Current.IsDarkTheme ? MarkupExtensions.GetImageFromString("Images/icon.png") : MarkupExtensions.GetImageFromString("Images/icon_dark.png");
                });

            zoomSubMenuItem = new MenuItem().AddSubItems(new List<UIElement>()
            {
                new MenuItem().InitMenuItem("_Zoom In").SetClick(handler: (s, e) => ZoomIn()),
                new MenuItem().InitMenuItem("_Zoom Out").SetClick(handler: (s, e) => ZoomOut()),
                new MenuItem().InitMenuItem("_Restore Default").SetClick(handler: (s, e) => ResetZoom()),
                new Separator() {FlowDirection = FlowDirection.RightToLeft}
            })
                .InitMenuItem("_Zoom");

            fileMenuItem = new MenuItem() { FontSize = 15 }.InitMenuItem("_File").AddSubItems(new List<UIElement>
            {
                new MenuItem().InitMenuItem("_New", click: New),
                new MenuItem().InitMenuItem("_Open", click: Open),
                new MenuItem().InitMenuItem("_Save", click: Save),
                new MenuItem().InitMenuItem("_Save As", click: SaveAs),
                new Separator(),
                new MenuItem().InitMenuItem("Print"),
                new Separator(),
                new MenuItem().InitMenuItem("Exit", click: (s,e) => App.Current.Shutdown()),
            }
                .ForEachUIElement((x) => x.SetValue(TextElement.FontSizeProperty, 12.0)));

            editMenuItem = new MenuItem() { FontSize = 15 }.InitMenuItem("Edit").AddSubItems(new List<UIElement>
            {
                new MenuItem().InitMenuItem("_Undo", click: (s, e) => txtBox.Undo()),
                new MenuItem().InitMenuItem("_Redo", click: (s, e) => txtBox.Redo()),
                new Separator(),
                new MenuItem().InitMenuItem("_Cut", click: (s, e) => txtBox.Selection.Text = "" ),
                new MenuItem().InitMenuItem("_Copy", click: (s,e) => Clipboard.SetText(txtBox.Selection.Text) ),
                new MenuItem().InitMenuItem("_Paste", click: (s, e) => txtBox.Paste() ),
                new Separator(),
                new MenuItem().InitMenuItem("_Find", click: (s, e) => ShowFind = true ),
                new MenuItem().InitMenuItem("_Replace", click: (s, e) => ShowFindAndReplace = true ),
                new MenuItem().InitMenuItem("_Go to"),
                new Separator(),
                new MenuItem().InitMenuItem("_Select All"),
                new Separator(),
                new MenuItem().InitMenuItem("_Font"),
            }
                .ForEachUIElement((x) => x.SetValue(TextElement.FontSizeProperty, 12.0)));

            viewMenuItem = new MenuItem() { FontSize = 15 }.InitMenuItem("_View").AddSubItems(new List<UIElement> { zoomSubMenuItem, statusBarThemeMenuChechBar, darkThemeMenuChechBar }
                .ForEachUIElement((x) => x.SetValue(TextElement.FontSizeProperty, 12.0)));

            menu = MarkupExtensions.CreateMenu(new UIElement[] { fileMenuItem, editMenuItem, viewMenuItem })
                .SetResourceReferenceAndReturnSelf(Menu.BackgroundProperty, Shell.WPF.UI.Brushes.Layer1BackgroundBrush)
                .SetValueAndReturnSelf(Grid.ColumnProperty, 0);
            #endregion

            lowerTitleBarContent = new Grid()
                .SetColumnDefinitions(new List<ColumnDefinition>
                {
                    new ColumnDefinition {},
                    new ColumnDefinition {}
                })
                .AddChildrenToGrid(menu, settingsIcon);

            windowContent = new Grid().AddChildrenToGrid(txtBox, findAndReplaceControl, settingsControl);

            bottomBarContent = new Grid { VerticalAlignment = VerticalAlignment.Center }.AddGridRowsAndCols(new RowDefinition[] { }, MarkupExtensions.FillWithType<ColumnDefinition>(4)).AddChildrenToGrid(new UIElement[]
            {
                new Rectangle {Width = 1, Margin = new(1), HorizontalAlignment= HorizontalAlignment.Right }
                .SetResourceReferenceAndReturnSelf(Rectangle.FillProperty, Shell.WPF.UI.Brushes.ForegroundBrush)
                .SetValueAndReturnSelf(Grid.ColumnProperty, 0),
                new Rectangle {Width = 1, Margin = new(1), HorizontalAlignment= HorizontalAlignment.Right }
                .SetResourceReferenceAndReturnSelf(Rectangle.FillProperty, Shell.WPF.UI.Brushes.ForegroundBrush)
                .SetValueAndReturnSelf(Grid.ColumnProperty, 1),
                new Rectangle {Width = 1, Margin = new(1), HorizontalAlignment= HorizontalAlignment.Right }
                .SetResourceReferenceAndReturnSelf(Rectangle.FillProperty, Shell.WPF.UI.Brushes.ForegroundBrush)
                .SetValueAndReturnSelf(Grid.ColumnProperty, 2),

                new TextBlock {FontSize = 15.0,HorizontalAlignment = HorizontalAlignment.Center,}
                .SetBindingAndReturnSelf(TextBlock.TextProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(Scale)), this, stringFormat: "{0:0.00} Zoom"))
                .SetValueAndReturnSelf(Grid.ColumnProperty, 3),
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _host.Start();

            NavigationShell.SetServiceProvider(_host.Services);



            MainWindow = new NavigationShell()
            {
                LowerTitleBarVisibility = Visibility.Visible,
                MenuIconVisibility = BarIconVisibility.Hidden,
                Icon = MarkupExtensions.GetImageFromString("Images/icon_dark.png"),
                Title = "Untitled",
                BottomBarVisibility = Visibility.Visible
            }
            .RunMethodAndReturnSelf(x =>
            {
                x.InputBindings.Add(new KeyBinding
                {
                    Key = Key.F,
                    Modifiers = ModifierKeys.Control,
                    Command = new RelayCommand(() =>
                    {
                        ShowFind = !ShowFind;
                    }, () => true)
                });
                x.InputBindings.Add(new KeyBinding
                {
                    Key = Key.H,
                    Modifiers = ModifierKeys.Control,
                    Command = new RelayCommand(() =>
                    {
                        ShowFindAndReplace = !ShowFindAndReplace;
                    }, () => true)
                });
            })
            .RunMethodAndReturnSelf(x =>
            {
                x.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        if (findBox.IsFocused)
                            FindText();

                        if (replaceBox.IsFocused)
                            ReplaceText();
                    }
                };
            })
            .SetBindingAndReturnSelf(NavigationShell.BottomBarVisibilityProperty, MarkupExtensions.CreateBinding(new PropertyPath(nameof(ShowStatusBar)), this, new BooleanToVisibilityConverter()))
            .SetResourceReferenceAndReturnSelf(NavigationShell.LowerTitleBarBackgroundProperty, Shell.WPF.UI.Brushes.Layer1BackgroundBrush)
            .SetResourceReferenceAndReturnSelf(NavigationShell.TitleBarBackgroundProperty, Shell.WPF.UI.Brushes.Layer1BackgroundBrush)
            .SetResourceReferenceAndReturnSelf(NavigationShell.BottomBarBackgroundProperty, Shell.WPF.UI.Brushes.Layer1BackgroundBrush)
            .SetProperty("BottomBarContent", bottomBarContent)
            .SetProperty("LowerTitleBarContent", lowerTitleBarContent)
            .SetProperty("Content", windowContent)
            .RunMethodAndReturnSelf((s) => s.Closing += (s, e) => SaveConfig() );
            

            if (_useDarkMode)
                NavigationShell.Current.ToggleTheme();

            MainWindow.Show();

            base.OnStartup(e);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
            base.OnExit(e);
        }

        private void ResetZoom()
        {
            Scale = 1.5;
            txtBox.LayoutTransform = new ScaleTransform(Scale, Scale);
        }

        private void ZoomOut()
        {
            if (Scale > 0.7)
            {
                Scale -= 0.1;
                txtBox.LayoutTransform = new ScaleTransform(Scale, Scale);
            }
        }

        private void ZoomIn()
        {
            if (Scale < max)
            {
                Scale += 0.1;
                txtBox.LayoutTransform = new ScaleTransform(Scale, Scale);
            }
        }

        private void OnTxtBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            var selection = txtBox.Selection;
            if (selection != null && foundMatches != null && foundMatches.Count > currentMatch)
            {
                var ss = selection.Start.GetTextInRun(LogicalDirection.Forward);
                var ms = foundMatchesRanges[currentMatch].Start.GetTextInRun(LogicalDirection.Forward);
                if (ss == ms)
                {
                    foundMatchesRanges[currentMatch].ApplyPropertyValue(TextElement.BackgroundProperty, TryFindResource(Shell.WPF.UI.Brushes.Layer0BackgroundBrush) as Brush);
                }

                if (selection.Start.CompareTo(foundMatchesRanges[currentMatch].Start) == 1 && selection.End.CompareTo(foundMatchesRanges[currentMatch].End) == -1)
                {
                    foundMatchesRanges[currentMatch].ApplyPropertyValue(TextElement.BackgroundProperty, TryFindResource(Shell.WPF.UI.Brushes.Layer0BackgroundBrush) as Brush);
                }

                var se = selection.End.GetTextInRun(LogicalDirection.Backward);
                var me = foundMatchesRanges[currentMatch].End.GetTextInRun(LogicalDirection.Backward);

                if (se == me)
                {
                    foundMatchesRanges[currentMatch].ApplyPropertyValue(TextElement.BackgroundProperty, TryFindResource(Shell.WPF.UI.Brushes.Layer0BackgroundBrush) as Brush);
                }

            }
        }

        private void OnTxtBoxGotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Open(object? sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = @"c:\";
            if (openFileDialog.ShowDialog() == true)
            {
                txtBox.Document.Blocks.Clear();
                txtBox.Document.Blocks.Add(new Paragraph(new Run(System.IO.File.ReadAllText(openFileDialog.FileName))));
                SetFile(openFileDialog.FileName);
            }
        }

        private void SetFile(string file)
        {
            File = file;
            FileInfo fileInfo = new FileInfo(file);
            NavigationShell.Current.Title = fileInfo.Name;
        }

        private void SaveAs(object? sender, RoutedEventArgs e) 
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();

            sfd.InitialDirectory = @"C:\";

            sfd.Filter = @"Text Files(*.txt)|*.txt|All(*.*)|*";

            if (sfd.ShowDialog() is true)
            {
                if (System.IO.File.Exists(sfd.FileName))
                {
                    System.IO.File.WriteAllText(sfd.FileName, new TextRange(txtBox.Document.ContentStart, txtBox.Document.ContentEnd).Text);
                }
                else
                {
                    System.IO.File.WriteAllText(sfd.FileName, new TextRange(txtBox.Document.ContentStart, txtBox.Document.ContentEnd).Text);
                }

                SetFile(sfd.FileName);
            }
            else
            {
                MessageBox.Show("Cancel save");
            }
        }

        private void Save(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(File))
            {
                SaveAs(sender, e);
                return;
            }

            System.IO.File.WriteAllText(File, new TextRange(txtBox.Document.ContentStart, txtBox.Document.ContentEnd).Text);
        }

        private void New(object? sender, RoutedEventArgs e)
        {

        }

        private void FindText()
        {
            if (foundMatchesRanges != null && foundMatchesRanges.Count > currentMatch)
            {
                var range = foundMatchesRanges[currentMatch];

                range.ApplyPropertyValue(TextElement.BackgroundProperty, TryFindResource(Shell.WPF.UI.Brushes.Layer0BackgroundBrush) as Brush);
            }

            if (string.IsNullOrWhiteSpace(findBox.Text))
                return;

            string allText = new TextRange(txtBox.Document.ContentStart, txtBox.Document.ContentEnd).Text;

            string[] words = allText.Split(' ');

            List<string> matches;

            if (matchCase && matchWholeWord || matchWholeWord)
                matches = words.Where(x => x == findBox.Text).ToList();
            else if (matchCase)
                matches = words.Where(x => x.Contains(findBox.Text)).ToList();
            else
                matches = words.Where(x => x.ToLower().Contains(findBox.Text.ToLower())).ToList();

            foundMatches = matches;
            FindTextRanges();
            GoToMatch();
        }

        private void ReplaceText()
        {

        }

        private void ReplaceAllText()
        {

        }

        private void FindTextRanges()
        {
            //txtBox.SelectAll();

            TextPointer position = txtBox.Document.ContentStart;

            List<TextRange> ranges = new List<TextRange>();
            string keyword = findBox.Text.Trim();
            Regex reg = new Regex(keyword, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string text = position.GetTextInRun(LogicalDirection.Forward);
                    var matches = reg.Matches(text);

                    foreach (Match match in matches)
                    {

                        TextPointer start = position.GetPositionAtOffset(match.Index);
                        TextPointer end = start.GetPositionAtOffset(keyword.Length);

                        TextRange textrange = new TextRange(start, end);
                        ranges.Add(textrange);
                    }
                }
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            foundMatchesRanges = ranges;
        }

        private void GoToMatch()
        {
            if (foundMatchesRanges != null && foundMatchesRanges.Count > currentMatch)
            {
                var range = foundMatchesRanges[currentMatch];

                txtBox.ScrollToVerticalOffset(range.End.GetCharacterRect(LogicalDirection.Forward).BottomRight.Y - 200);

                range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.LightBlue);
            }

            if (foundMatchesRanges == null || foundMatchesRanges.Count <= currentMatch)
            {
                MessageBox.Show(MainWindow, $"Cannot find \"{findBox.Text}\"", "Notepad");
                currentMatch = 0;
            }
        }

        private void ReplaceMatch()
        {
            if (foundMatchesRanges != null && foundMatchesRanges.Count > currentMatch && !String.IsNullOrWhiteSpace(replaceBox.Text))
            {
                var range = foundMatchesRanges[currentMatch];
                txtBox.ScrollToVerticalOffset(range.End.GetCharacterRect(LogicalDirection.Forward).BottomRight.Y);
                range.Text = replaceBox.Text;
            }
        }

        private void ReplaceAllMatches()
        {
            FindText();
            while (foundMatchesRanges != null && foundMatchesRanges.Count > currentMatch)
            {
                ReplaceMatch();
                currentMatch++;
            }
        }

        private void GoToNextMatch()
        {
            if (foundMatchesRanges != null && foundMatchesRanges.Count > currentMatch)
            {
                var range = foundMatchesRanges[currentMatch];
                range.ApplyPropertyValue(TextElement.BackgroundProperty, TryFindResource(Shell.WPF.UI.Brushes.Layer0BackgroundBrush) as Brush);
            }
            currentMatch++;
            GoToMatch();
        }

        private void GoToPreviousMatch()
        {
            if (foundMatchesRanges != null && foundMatchesRanges.Count > currentMatch)
            {
                var range = foundMatchesRanges[currentMatch];
                range.ApplyPropertyValue(TextElement.BackgroundProperty, TryFindResource(Shell.WPF.UI.Brushes.Layer0BackgroundBrush) as Brush);
            }
            currentMatch--;
            GoToMatch();
        }

        private int GetWordCount()
        {
            var text = new TextRange(txtBox.Document.ContentStart, txtBox.Document.ContentEnd).Text;

            return text.Split(' ').Count();
        }

        private void SaveConfig()
        {
            string json = "";

            json = $"{{\"Settings\": {{ \"FontSize\": {FontSize}, \"FontWeight\": \"{FontWeight}\", \"FontFamily\": \"{fontFamily}\", \"UseDarkMode\": {NavigationShell.Current.IsDarkTheme.ToString().ToLowerInvariant()}, \"ShowStatusBar\": {ShowStatusBar.ToString().ToLowerInvariant()} }}}}";

            string? path = null;
            if (System.IO.File.Exists(_path1a))
                path = _path1a;
            else if (System.IO.File.Exists(_path2))
                path = _path2;
            if (path != null)
                System.IO.File.WriteAllText(_path2, json);
        }

    }

    public static class TempExtensions
    {
        public static T SetContent<T>(this T c, object content) where T : ContentControl
        {
            c.Content = content;
            return c;
        }

        public static Grid SetColumnDefinitions(this Grid g, params ColumnDefinition[] columns)
        {
            foreach (ColumnDefinition column in columns)
            {
                g.ColumnDefinitions.Add(column);
            }

            return g;
        }

        public static List<int> GetNumCollection(int start, int end)
        {
            List<int> ints = new();

            while (end >= start)
            {
                ints.Add(end);
                end--;
            }

            ints.Reverse();

            return ints;
        }

        public static List<double> GetNumCollection(double start, double end)
        {
            List<double> ints = new();

            while (end >= start)
            {
                ints.Add(end);
                end--;
            }

            ints.Reverse();

            return ints;
        }

        public static List<long> GetNumCollection(long start, long end)
        {
            List<long> ints = new();

            while (end >= start)
            {
                ints.Add(end);
                end--;
            }

            ints.Reverse();

            return ints;
        }

    }

    public class BooleanToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (b)
                    return 100.0;
                return 40.0;
            }

            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToRowSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (b)
                    return 1;
                return 2;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToIconKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                if (b)
                    return PackIconMaterialDesignKind.ExpandLess;
                return PackIconMaterialDesignKind.ExpandMore;
            }

            return PackIconMaterialDesignKind.AccessAlarm;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
