﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ModernWpf.Controls
{
    /// <summary>
    /// Represents a specialized context menu that contains commands for editing text.
    /// </summary>
    public class TextContextMenu : ContextMenu
    {
        private static readonly CommandBinding _selectAllBinding;

        private readonly MenuItem _proofingMenuItem;

        static TextContextMenu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextContextMenu), new FrameworkPropertyMetadata(typeof(TextContextMenu)));

            _selectAllBinding = new CommandBinding(ApplicationCommands.SelectAll);
            _selectAllBinding.PreviewCanExecute += OnSelectAllPreviewCanExecute;
        }

        /// <summary>
        /// Initializes a new instance of the TextContextMenu class.
        /// </summary>
        public TextContextMenu()
        {
            _proofingMenuItem = new MenuItem
            {
                Header = Strings.Resources.ProofingMenuItemLabel
            };
            Items.Add(_proofingMenuItem);
            Items.Add(new MenuItem
            {
                Command = ApplicationCommands.Cut,
                Icon = new SymbolIcon(Symbol.Cut)
            });
            Items.Add(new MenuItem
            {
                Command = ApplicationCommands.Copy,
                Icon = new SymbolIcon(Symbol.Copy)
            });
            Items.Add(new MenuItem
            {
                Command = ApplicationCommands.Paste,
                Icon = new SymbolIcon(Symbol.Paste)
            });
            Items.Add(new MenuItem
            {
                Command = ApplicationCommands.Undo,
                Icon = new SymbolIcon(Symbol.Undo)
            });
            Items.Add(new MenuItem
            {
                Command = ApplicationCommands.Redo,
                Icon = new SymbolIcon(Symbol.Redo)
            });
            Items.Add(new MenuItem
            {
                Command = ApplicationCommands.SelectAll
            });
        }

        #region UsingTextContextMenu

        public static readonly DependencyProperty UsingTextContextMenuProperty =
            DependencyProperty.RegisterAttached(
                "UsingTextContextMenu",
                typeof(bool),
                typeof(TextContextMenu),
                new PropertyMetadata(false, OnUsingTextContextMenuChanged));

        public static bool GetUsingTextContextMenu(Control textControl)
        {
            return (bool)textControl.GetValue(UsingTextContextMenuProperty);
        }

        public static void SetUsingTextContextMenu(Control textControl, bool value)
        {
            textControl.SetValue(UsingTextContextMenuProperty, value);
        }

        private static void OnUsingTextContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textControl = (Control)d;
            if ((bool)e.NewValue)
            {
                textControl.CommandBindings.Add(_selectAllBinding);
                textControl.ContextMenuOpening += OnContextMenuOpening;
            }
            else
            {
                textControl.CommandBindings.Remove(_selectAllBinding);
                textControl.ContextMenuOpening -= OnContextMenuOpening;
            }
        }

        #endregion

        protected override void OnOpened(RoutedEventArgs e)
        {
            base.OnOpened(e);

            if (_proofingMenuItem.IsVisible)
            {
                _proofingMenuItem.IsSubmenuOpen = true;
            }
        }

        protected override void OnClosed(RoutedEventArgs e)
        {
            base.OnClosed(e);

            _proofingMenuItem.Items.Clear();

            foreach (MenuItem menuItem in Items)
            {
                menuItem.ClearValue(MenuItem.CommandTargetProperty);
            }
        }

        private static void OnSelectAllPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                e.CanExecute = false;
                e.Handled = true;
            }
            else if (sender is PasswordBox passwordBox && string.IsNullOrEmpty(passwordBox.Password))
            {
                e.CanExecute = false;
                e.Handled = true;
            }
        }

        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var textControl = (Control)sender;
            if (textControl.ContextMenu is TextContextMenu contextMenu)
            {
                contextMenu.UpdateItems(textControl);

                bool hasVisibleItems = contextMenu.Items.OfType<MenuItem>().Any(mi => mi.Visibility == Visibility.Visible);
                if (!hasVisibleItems)
                {
                    e.Handled = true;
                }
            }
        }

        private void UpdateProofingMenuItem(Control target)
        {
            _proofingMenuItem.Items.Clear();

            SpellingError spellingError = null;

            if (target is TextBox textBox)
            {
                spellingError = textBox.GetSpellingError(textBox.CaretIndex);
            }
            else if (target is RichTextBox richTextBox)
            {
                spellingError = richTextBox.GetSpellingError(richTextBox.CaretPosition);
            }

            if (spellingError != null)
            {
                foreach (string suggestion in spellingError.Suggestions)
                {
                    var menuItem = new MenuItem
                    {
                        Header = suggestion,
                        Command = EditingCommands.CorrectSpellingError,
                        CommandParameter = suggestion,
                        CommandTarget = target
                    };
                    _proofingMenuItem.Items.Add(menuItem);
                }

                if (_proofingMenuItem.HasItems)
                {
                    _proofingMenuItem.Items.Add(new Separator());
                }

                _proofingMenuItem.Items.Add(new MenuItem
                {
                    Header = "Ignore",
                    Command = EditingCommands.IgnoreSpellingError,
                    CommandTarget = target
                });

                _proofingMenuItem.Visibility = Visibility.Visible;
            }
            else
            {
                _proofingMenuItem.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateItems(Control target)
        {
            UpdateProofingMenuItem(target);

            foreach (MenuItem menuItem in Items)
            {
                if (menuItem.Command is RoutedUICommand command)
                {
                    menuItem.CommandTarget = target;
                    menuItem.Visibility = command.CanExecute(null, target) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }
}
