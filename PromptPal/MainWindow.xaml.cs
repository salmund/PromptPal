using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PromptPal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DatabaseService _dbService;
        private ObservableCollection<Prompt> _searchResults;

        public MainWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _dbService.InitializeDatabase(); // Assurez-vous que la base de données est prête

            // Initialisation de la collection pour la ListBox
            _searchResults = new ObservableCollection<Prompt>();
            ResultsList.ItemsSource = _searchResults;

            // Ajouter quelques prompts de test
            _dbService.AddPrompt(new Prompt { Title = "Améliorer Image", Content = "Détail, haute résolution, couleurs vibrantes, style cinématographique, lumière douce." });
            _dbService.AddPrompt(new Prompt { Title = "Brainstorming Idées", Content = "Génère 5 idées originales pour un nouveau produit dans le domaine de la technologie verte, avec une analyse SWOT rapide pour chaque." });
            _dbService.AddPrompt(new Prompt { Title = "Résumé de texte", Content = "Résume le texte suivant en 3 points clés, en utilisant un langage clair et concis." });

            this.Loaded += MainWindow_Loaded; // Appliquer le flou après le chargement
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Appliquer l'effet de flou "Acrylic" de Windows 11
            // Cela est plus complexe et nécessite des appels P/Invoke spécifiques au DWM (Desktop Window Manager).
            // Voici une version simplifiée, pour un flou complet, vous aurez besoin de plus de code.
            // Pour un vrai effet Acrylic, des bibliothèques comme MahApps.Metro ou FluentWPF peuvent aider.
            // Ou manuellement:
            EnableBlur(this);
        }

        // Méthode simplifiée pour activer le flou DWM (nécessite plus de P/Invoke pour être complet)
        // Pour un effet Acrylic parfait, c'est plus complexe.
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ... autres attributs ...
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3, // C'est celui-ci qui nous intéresse pour un flou simple
            ACCENT_ENABLE_ACRYLIC = 4, // Acrylic!
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal void EnableBlur(Window window)
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND; // Ou ACCENT_ENABLE_ACRYLIC

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        // ... (Le HwndHook pour le raccourci global reste le même) ...

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text;
            // Utilisez BeginInvoke pour ne pas bloquer l'UI si la recherche est longue
            // C'est un peu plus complexe avec async/await pour éviter les Deadlocks sur UI Thread
            // Mais pour SQLite, c'est généralement rapide.

            var results = await Task.Run(() => _dbService.SearchPrompts(searchText));
            _searchResults.Clear();
            foreach (var prompt in results)
            {
                _searchResults.Add(prompt);
            }
            if (ResultsList.Items.Count > 0)
            {
                ResultsList.SelectedIndex = 0; // Sélectionne le premier élément par défaut
            }
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (ResultsList.Items.Count > 0 && ResultsList.SelectedIndex < ResultsList.Items.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                e.Handled = true; // Empêche l'événement de se propager
            }
            else if (e.Key == Key.Up)
            {
                if (ResultsList.Items.Count > 0 && ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CopySelectedPromptAndHide();
                e.Handled = true;
            }
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CopySelectedPromptAndHide();
                e.Handled = true;
            }
        }

        private void CopySelectedPromptAndHide()
        {
            if (ResultsList.SelectedItem is Prompt selectedPrompt)
            {
                Clipboard.SetText(selectedPrompt.Content);
                this.Hide();
                SearchBox.Text = string.Empty; // Réinitialise la recherche
                _searchResults.Clear(); // Vide la liste
            }
        }

        // Override OnDeactivated pour cacher la fenêtre si elle perd le focus
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if (this.IsVisible)
            {
                this.Hide();
                SearchBox.Text = string.Empty;
                _searchResults.Clear();
            }
        }
    }
}