﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Revsoft.Wabbitcode.Dialogs;
using Revsoft.Wabbitcode.Interface;
using Revsoft.Wabbitcode.Interface.Services;
using Revsoft.Wabbitcode.Panels;
using Revsoft.Wabbitcode.Services;
using Revsoft.Wabbitcode.Utilities;

namespace Revsoft.Wabbitcode
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region IService

		IAssemblerService AssemblerService { get; set; }
		IDockingService DockingService { get; set; }
		IDocumentService DocumentService { get; set; }
		IPathsService PathsService { get; set; }
		IProjectService ProjectService { get; set; }
		
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		#endregion

		#region Panels Visibility

		public bool LabelListVisible
		{
			get
			{
				if (DockingService == null || DockingService.LabelList == null)
					return false;
				return DockingService.LabelList.State != AvalonDock.DockableContentState.Hidden;
			}
			set 
			{
				if (DockingService.LabelList.State == AvalonDock.DockableContentState.Hidden)
				{
					DockingService.LabelList.Show();
				}
				else
				{
					DockingService.LabelList.Hide();
				}
			}
		}

		public bool ProjectViewerVisible
		{
			get
			{
				if (DockingService == null || DockingService.ProjectViewer == null)
					return false;
				return DockingService.ProjectViewer.State != AvalonDock.DockableContentState.Hidden;
			}
			set
			{
				if (DockingService.ProjectViewer.State == AvalonDock.DockableContentState.Hidden)
				{
					DockingService.ProjectViewer.Show();
				}
				else
				{
					DockingService.ProjectViewer.Hide();
				}
			}
		}

		public bool OutputWindowVisible
		{
			get
			{
				if (DockingService == null || DockingService.OutputWindow == null)
					return false;
				return DockingService.OutputWindow.State != AvalonDock.DockableContentState.Hidden;
			}
			set
			{
				if (DockingService.OutputWindow.State == AvalonDock.DockableContentState.Hidden)
				{
					DockingService.OutputWindow.Show();
				}
				else
				{
					DockingService.OutputWindow.Hide();
				}
			}
		}

		public bool ErrorListVisible
		{
			get
			{
				if (DockingService == null || DockingService.ErrorList == null)
					return false;
				return DockingService.ErrorList.State != AvalonDock.DockableContentState.Hidden;
			}
			set
			{
				if (DockingService.ErrorList.State == AvalonDock.DockableContentState.Hidden)
				{
					DockingService.ErrorList.Show();
				}
				else
				{
					DockingService.ErrorList.Hide();
				}
			}
		}

		#endregion

		readonly string[] args;
		public MainWindow(string[] args)
		{
			InitializeComponent();
			this.args = args;

			DataContext = this;
		}

		#region Startup
		public void HandleArgs(string[] args)
		{
			if (args.Length == 0)
				return;
			foreach (string file in args)
				if (File.Exists(file))
					if (Path.GetExtension(file) == ".wcodeproj")
					{
						ProjectService.OpenProject(file);
						return;
					}
					else
					{
						foreach (string arg in args)
							try
							{
								if (string.IsNullOrEmpty(arg))
									break;
								DocumentService.OpenDocument(arg);
							}
							catch (FileNotFoundException)
							{
								Services.DockingService.ShowError("Error: File not found!");
							}
							catch (Exception ex)
							{
								Services.DockingService.ShowError("Error in loading startup args", ex);
							}
					}
		}
		#endregion

		#region Form Methods
		private void Window_Closed(object sender, EventArgs e)
		{
			//Clean up and save panels and window pos
			ServiceFactory.Instance.DestroyServiceInstance((IService) DockingService);
		}

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
			DockingService = ServiceFactory.Instance.GetServiceInstance<DockingService>(dockManager, this);
			DocumentService = ServiceFactory.Instance.GetServiceInstance<DocumentService>();
			AssemblerService = ServiceFactory.Instance.GetServiceInstance<AssemblerService>();
			PathsService = ServiceFactory.Instance.GetServiceInstance<PathsService>();
			ProjectService = ServiceFactory.Instance.GetServiceInstance<ProjectService>();
		}

		private void dockManager_Loaded(object sender, RoutedEventArgs e)
		{
			DockingService.InitPanels(StatusBar);

			//handle any arguments sent
			HandleArgs(args);
		}
		#endregion

		#region File Menu
		private void NewFile_Executed(object sender, RoutedEventArgs e)
		{
			var newFile = DocumentService.CreateDocument("New Document");
			DockingService.ShowDockPanel(newFile);
			Keyboard.Focus(newFile.editor.TextArea);
		}

		private void NewProject_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var dialog = new NewProjectDialog();
			dialog.ShowDialog();
		}

		private void OpenFile_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			DocumentService.OpenDocument();
		}

		private void OpenProject_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var ofn = new OpenFileDialog()
			{
				AddExtension = true,
				CheckFileExists = true,
				DefaultExt = ".wcodeproj",
				Filter = "Project Files (*.wcodeproj)|*.wcodeproj|All Files(*.*)|*.*",
				FilterIndex = 0,
				InitialDirectory = PathsService.ProjectDirectory,
				Multiselect = false,
				RestoreDirectory = true,
				Title = "Open Project",
			};
			if (ofn.ShowDialog() == true)
			{
				ProjectService.OpenProject(ofn.FileName);
			}
		}

		private void SaveFile_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActiveDocument != null)
				DockingService.ActiveDocument.SaveFile();
		}

		private void SaveAll_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			foreach(Editor doc in DockingService.Documents)
				doc.SaveFile();
		}

		private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActiveDocument != null)
				DockingService.ActiveDocument.Close();
		}
		#endregion

		#region Edit Menu
		private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActivePanel != null)
				DockingService.ActivePanel.Undo();
		}

		private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActivePanel != null)
				DockingService.ActivePanel.Redo();
		}

		private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActivePanel != null)
				DockingService.ActivePanel.Cut();
		}

		private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActivePanel != null)
				DockingService.ActivePanel.Copy();
		}

		private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActivePanel != null)
				DockingService.ActivePanel.Paste();
		}

		private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (DockingService.ActiveDocument != null)
				DockingService.ActiveDocument.SelectAll();
		}
		#endregion

		private void Assemble_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var filePath = DockingService.ActiveDocument.FilePath;
			var outputPath = new FilePath(Path.ChangeExtension(filePath, ".8xk"));
			AssemblerService.AssembleFile(filePath, outputPath);
		}

		private void About_Executed(object snder, ExecutedRoutedEventArgs e)
		{
			var aboutBox = new AboutBox();
			aboutBox.ShowDialog();
		}
	}
}