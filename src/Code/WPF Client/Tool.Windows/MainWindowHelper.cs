﻿#region Usings

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Microsoft.Web.Administration;
using SIM.Base;
using SIM.Instances;
using SIM.Pipelines;
using SIM.Pipelines.Agent;
using SIM.Pipelines.Install;
using SIM.Pipelines.Reinstall;
using SIM.Products;
using SIM.Tool.Base;
using SIM.Tool.Base.Plugins;
using SIM.Tool.Base.Profiles;
using SIM.Tool.Wizards;
using MenuItem = System.Windows.Controls.MenuItem;
using Product = SIM.Products.Product;
using Fluent;

#endregion

namespace SIM.Tool.Windows
{
  #region



  #endregion

  /// <summary>
  ///   The main window helper.
  /// </summary>
  public static class MainWindowHelper
  {
    /// <summary>
    /// The reinstall instance.
    /// </summary>
    /// <param name="instance">
    /// The instance. 
    /// </param>
    /// <param name="owner">
    /// The owner.
    /// </param>
    /// <param name="license">
    /// The license. 
    /// </param>
    /// <param name="connectionString">
    /// The connection string. 
    /// </param>
    public static void ReinstallInstance([NotNull] Instance instance, Window owner, [NotNull] string license, [NotNull] SqlConnectionStringBuilder connectionString)
    {
      Assert.ArgumentNotNull(instance, "instance");
      Assert.ArgumentNotNull(license, "license");
      Assert.ArgumentNotNull(connectionString, "connectionString");

      if (instance.IsSitecore)
      {
        Product product = instance.Product;
        if (string.IsNullOrEmpty(product.PackagePath))
        {
          if (WindowHelper.ShowMessage("The {0} product isn't presented in your local repository. Would you like to choose the zip installation package?".FormatWith(instance.ProductFullName), MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.Yes)
          {
            string patt = instance.ProductFullName + ".zip";
            OpenFileDialog fileBrowserDialog = new OpenFileDialog { Title = @"Choose installation package", Multiselect = false, CheckFileExists = true, Filter = patt + '|' + patt };

            if (fileBrowserDialog.ShowDialog() == DialogResult.OK)
            {
              product = Product.Parse(fileBrowserDialog.FileName);
              if (string.IsNullOrEmpty(product.PackagePath))
              {
                WindowHelper.HandleError("SIM can't parse the {0} package".FormatWith(instance.ProductFullName), true, null, typeof(MainWindowHelper));
                return;
              }
            }
          }
        }

        if (string.IsNullOrEmpty(product.PackagePath))
        {
          return;
        }

        ReinstallArgs args;
        try
        {
          args = new ReinstallArgs(instance, connectionString, license, SIM.Pipelines.Install.Settings.CoreInstallWebServerIdentity.Value);
        }
        catch (Exception ex)
        {
          WindowHelper.HandleError(ex.Message, false, ex, typeof(WindowHelper));
          return;
        }
        var name = instance.Name;
        WizardPipelineManager.Start("reinstall", owner, args, null, () => MainWindowHelper.MakeInstanceSelected(name));
      }
    }
    /// <summary>
    /// The refresh products.
    /// </summary>
    public static void RefreshInstaller()
    {
      using (new ProfileSection("Refresh installer", typeof(MainWindowHelper)))
      {
        var mainWindow = MainWindow.Instance;
        DisableInstallButtons(mainWindow);
        DisableRefreshButton(mainWindow);
        WindowHelper.LongRunningTask(RefreshInstallerTask, "Initialization", mainWindow, "Scanning local repository to find supported product packages", "The supported product packages are *.zip files they could be Sitecore packages, standalone packages or regular archive files. For supported files it computes manifests with information how the files should be treated.\n\nThe very first time the operation may take quite a long time, or if you clicked Refresh -> Everything", true);
        EnableRefreshButton(mainWindow);
      }
    }

    private static void DisableInstallButtons(MainWindow mainWindow)
    {
      mainWindow.HomeTabInstallGroup.IsEnabled = false;
    }

    private static void DisableRefreshButton(MainWindow mainWindow)
    {
      mainWindow.HomeTabRefreshGroup.IsEnabled = false;
    }

    public static void EnableRefreshButton(MainWindow mainWindow)
    {
      mainWindow.HomeTabRefreshGroup.IsEnabled = true;
    }

    private static void RefreshInstallerTask()
    {
      string message = InitializeInstallerUnsafe(MainWindow.Instance);
      MainWindowHelper.Invoke((mainWindow) => MainWindowHelper.UpdateInstallButtons(message, mainWindow));
      if (message != null)
      {
        WindowHelper.HandleError("Cannot find any installation package. " + message, false, null, typeof(MainWindowHelper));
      }
    }


    /// <summary>
    ///   Initializes the installer unsafe.
    /// </summary>
    public static string InitializeInstallerUnsafe(Window window)
    {
      using (new ProfileSection("Initialize Installer (Unsafe)", typeof(MainWindowHelper)))
      {
        string message = null;
        string localRepository = ProfileManager.Profile.LocalRepository;

        try
        {
          ProductManager.Initialize(localRepository);
        }
        catch (Exception ex)
        {
          SIM.Base.Log.Error("Installer failed to init. {0}".FormatWith(ex.Message), typeof(MainWindowHelper), ex);
          message = ex.Message;
        }

        return message;
      }
    }

    public static void UpdateInstallButtons(string message, MainWindow mainWindow)
    {
      mainWindow.HomeTabInstallGroup.IsEnabled = message == null;
      EnableRefreshButton(mainWindow);
      if (message != null)
      {
        WindowHelper.HandleError("Refresh failed ... " + message, false);
      }
    }

    #region Properties

    /// <summary>
    ///   Gets the selected instance.
    /// </summary>
    [CanBeNull]
    public static Instance SelectedInstance
    {
      get
      {
        return MainWindow.Instance.InstanceList.SelectedValue as Instance;
      }
    }

    #endregion

    #region Public methods

    #endregion

    #region Methods

    #region Fields


    #endregion

    /// <summary>
    ///   Closes the main window.
    /// </summary>
    public static void CloseMainWindow()
    {
      MainWindow.Instance.Dispatcher.InvokeShutdown();
      MainWindow.Instance.Close();
    }

    public static int GetListItemID(long value)
    {
      var itemCollection = MainWindow.Instance.InstanceList.Items;

      for (int i = 0; i < itemCollection.Count; ++i)
      {
        if (((Instance)itemCollection[i]).ID == value)
        {
          return i;
        }
      }

      // YBO: Fix for issue #37. If we haven't found the ID of a newly installed instance, we should refresh the list.
      RefreshInstances();

      for (int i = 0; i < itemCollection.Count; ++i)
      {
        if (((Instance)itemCollection[i]).ID == value)
    {
          return i;
        }
      }

      throw new ArgumentOutOfRangeException("There is no instance with {0} ID in the list".FormatWith(value));
    }

    public static void MakeInstanceSelected(int id)
    {
      var count = MainWindow.Instance.InstanceList.Items.Count;
      if (count == 0) return;
      if (id >= count)
      {
        MakeInstanceSelected(count - 1);
        return;
      }
      if (id < 0)
      {
        MakeInstanceSelected(0);
        return;
      }

      MainWindow.Instance.InstanceList.SelectedItem = MainWindow.Instance.InstanceList.Items[id];
      FocusManager.SetFocusedElement(MainWindow.Instance.InstanceList, MainWindow.Instance.InstanceList);
    }

    /// <summary>
    ///   Instances the selected.
    /// </summary>
    public static void OnInstanceSelected()
    {
      using (new ProfileSection("Main window instance selected handler", typeof(MainWindowHelper)))
      {
        if (SelectedInstance != null && MainWindow.Instance.HomeTab.IsSelected)
        {
          MainWindow.Instance.OpenTab.IsSelected = true;
        }
      }
    }

    public static void ChangeAppPoolMode(MenuItem menuItem)
    {
      var selectedInstance = SelectedInstance;
      WindowHelper.LongRunningTask(() => MainWindow.Instance.Dispatcher.Invoke(
        new Action(delegate
                     {
                       string header = menuItem.Header.ToString();
                       selectedInstance.SetAppPoolMode(header.Contains("4.0"), header.Contains("32bit"));
                       OnInstanceSelected();
                     })), "Changing application pool", MainWindow.Instance, null, "The IIS metabase is being updated");
    }

    //private static void SetupInstanceRestoreButton(string webRootPath)
    //{
    //  using (new ProfileSection("MainWindowHelper:SetupInstanceRestoreButton()"))
    //  {
    //    //MainWindow.Instance.rsbRestore.Items.Clear();

    //    try
    //    {
    //      string backupsFolder;
    //      using (new ProfileSection("MainWindowHelper:SetupInstanceRestoreButton(), backupsFolder"))
    //      {
    //        backupsFolder = SelectedInstance.GetBackupsFolder(webRootPath);
    //      }
    //      bool hasBackups;
    //      using (new ProfileSection("MainWindowHelper:SetupInstanceRestoreButton(), hasBackups"))
    //      {
    //        hasBackups = FileSystem.Instance.DirectoryExists(backupsFolder) &&
    //                     FileSystem.Instance.GetDirectories(backupsFolder, "*", SearchOption.TopDirectoryOnly).Length > 0;
    //      }
    //      MainWindow.Instance.rsbRestore.IsEnabled = hasBackups;
    //    }
    //    catch (InvalidOperationException ex)
    //    {
    //      Log.Warn(ex.Message, typeof(MainWindowHelper), ex);
    //      MainWindow.Instance.rsbRestore.IsEnabled = false;
    //    }
    //  }
    //}

    /// <summary>
    /// Opens the folder.
    /// </summary>
    /// <param name="path">
    /// The path. 
    /// </param>
    public static void OpenFolder([NotNull] string path)
    {
      Assert.ArgumentNotNull(path, "path");

      if (FileSystem.Local.Directory.Exists(path))
      {
        WindowHelper.OpenFolder(path);
      }
    }

    /// <summary>
    ///   Searches this instance.
    /// </summary>
    public static void Search()
    {
      using (new ProfileSection("Main window search handler", typeof(MainWindowHelper)))
      {
      string searchPhrase = Invoke(w => w.SearchTextBox.Text.Trim());
      IEnumerable<Instance> source = InstanceManager.PartiallyCachedInstances;
      if (source == null)
      {
          return;
      }

      //source = source.Select(inst => new CachedInstance((int)inst.ID));

      if (!string.IsNullOrEmpty(searchPhrase))
      {
        source = source.Where(instance => IsInstanceMatch(instance, searchPhrase));
      }

        source = source.OrderBy(instance => instance.Name);
        MainWindow.Instance.InstanceList.DataContext = source;
        MainWindow.Instance.SearchTextBox.Focus();
      }
    }

    private static bool IsInstanceMatch(Instance instance, string searchPhrase)
    {
      return instance.Name.ContainsIgnoreCase(searchPhrase) || instance.ProductFullName.ContainsIgnoreCase(searchPhrase) || instance.Product.SearchToken.ContainsIgnoreCase(searchPhrase);
    }

    public static T Invoke<T>(Func<MainWindow, T> func) where T : class
    {
      var window = MainWindow.Instance;
      T result = null;
      window.Dispatcher.Invoke(new Action(() => { result = func(window); }));
      return result;
    }

    public static void Invoke(Action<MainWindow> func)
    {
      var window = MainWindow.Instance;
      window.Dispatcher.Invoke(new Action(() => func(window)));
    }

    /*public class CachedInstance : Instance
    {
      private string name;
      private string webRootPath;

      public CachedInstance(int id)
        : base(id)
      {
      }

      public override string Name
      {
        get
        {
          return this.name ?? (this.name = base.Name);
        }
      }

      public override string WebRootPath
      {
        get
        {
          return this.webRootPath ?? (this.webRootPath = base.WebRootPath);
        }
      }
    }*/

    #endregion

    public static void MakeInstanceSelected(string name)
    {
      var id = GetListItemID(name);
      MakeInstanceSelected(id);
    }

    private static int GetListItemID(string value)
    {
      var itemCollection = MainWindow.Instance.InstanceList.Items;
      for (int i = 0; i < itemCollection.Count; ++i)
      {
        if (((Instance)itemCollection[i]).Name == value)
        {
          return i;
        }
      }

      throw new ArgumentOutOfRangeException("There is no instance with {0} ID in the list".FormatWith(value));
    }

    public static void OpenProgramLogs()
    {
      WindowHelper.OpenFolder(SIM.Base.Log.LogsFolder);
    }

    public static void KillProcess(Instance instance = null)
    {
      instance = instance ?? SelectedInstance;

      if (instance != null)
      {
        foreach (var id in instance.ProcessIds)
        {
          Process process = Process.GetProcessById((int)id);
          Log.Info("Kill the w3wp.exe worker process ({0}) of the {1} instance".FormatWith(id, instance.Name), typeof(MainWindowHelper));
          process.Kill();
          OnInstanceSelected();
        }
      }
    }

    public static void AppPoolRecycle()
    {
      if (SelectedInstance != null)
      {
        SelectedInstance.Recycle();
        OnInstanceSelected();
      }
    }

    public static void AppPoolStart()
    {
      if (SelectedInstance != null)
      {
        SelectedInstance.Start();
        OnInstanceSelected();
      }
    }

    public static void AppPoolStop()
    {
      if (SelectedInstance != null)
      {
        SelectedInstance.Stop();
        OnInstanceSelected();
      }
    }

    public static void OpenManifestsFolder()
    {
      OpenFolder("Manifests");
    }

    #region Plugins

    private static void InitializeRibbonTab(XmlElement tabElement, MainWindow window, Func<string, ImageSource> getImage)
    {
      var name = tabElement.GetNonEmptyAttribute("name");
      if (string.IsNullOrEmpty(name))
      {
        Log.Error("Ribbon tab doesn't have name: " + tabElement.OuterXml, typeof(MainWindowHelper));
        return;
      }

      using (new ProfileSection("Initialize ribbon tab", typeof(MainWindowHelper)))
      {
        ProfileSection.Argument("name", name);

        var tabName = name + "Tab";
        var ribbonTab = window.FindName(tabName) as RibbonTabItem ?? CreateTab(window, name);
        Assert.IsNotNull(ribbonTab, "Cannot find RibbonTab with {0} name".FormatWith(tabName));

        var groups = SelectNonEmptyCollection(tabElement, "group");
        foreach (XmlElement groupElement in groups)
        {
          InitializeRibbonGroup(name, tabName, groupElement, ribbonTab, window, getImage);
        }
      }
    }

    private static void InitializeRibbonGroup(string name, string tabName, XmlElement groupElement, RibbonTabItem ribbonTab, MainWindow window, Func<string, ImageSource> getImage)
    {
      using (new ProfileSection("Initialize ribbon group", typeof(MainWindowHelper)))
      {
        ProfileSection.Argument("name", name);
        ProfileSection.Argument("tabName", tabName);
        ProfileSection.Argument("groupElement", groupElement);
        ProfileSection.Argument("ribbonTab", ribbonTab);
        ProfileSection.Argument("window", window);
        ProfileSection.Argument("getImage", getImage);

        // Get Ribbon Group to insert button to
        name = groupElement.GetNonEmptyAttribute("name");
        var groupName = tabName + name + "Group";
        var ribbonGroup = GetRibbonGroup(name, tabName, groupName, ribbonTab, window);

        Assert.IsNotNull(ribbonGroup, "Cannot find RibbonGroup with {0} name".FormatWith(groupName));

        var buttons = SelectNonEmptyCollection(groupElement, "button");
        foreach (var button in buttons)
        {
          InitializeRibbonButton(window, getImage, button, ribbonGroup);
        }
      }
    }

    private static RibbonGroupBox GetRibbonGroup(string name, string tabName, string groupName, RibbonTabItem ribbonTab, MainWindow window)
    {
      using (new ProfileSection("Get ribbon group", typeof(MainWindowHelper)))
      {
        ProfileSection.Argument("name", name);
        ProfileSection.Argument("tabName", tabName);
        ProfileSection.Argument("groupName", groupName);
        ProfileSection.Argument("ribbonTab", ribbonTab);
        ProfileSection.Argument("window", window);

        var ribbonGroup = (window.FindName(groupName) as RibbonGroupBox);

        if (ribbonGroup == null)
        {
          var ribbonTabItem = window.FindName(tabName) as RibbonTabItem;

          if (ribbonTabItem != null)
          {
            var ribbonGroupBoxs = ribbonTabItem.Groups;
            foreach (
              var ribbonGroupBox in ribbonGroupBoxs.Where(ribbonGroupBox => ribbonGroupBox.Header.ToString() == name))
            {
              ribbonGroup = ribbonGroupBox;
              break;
            }
          }

          if (ribbonGroup == null) ribbonGroup = CreateGroup(ribbonTab, name);
        }
        return ribbonGroup;
      }
    }

    private static void InitializeRibbonButton(MainWindow window, Func<string, ImageSource> getImage, XmlElement button, RibbonGroupBox ribbonGroup)
    {
      using (new ProfileSection("Initialize ribbon button", typeof(MainWindowHelper)))
      {
        ProfileSection.Argument("button", button);
        ProfileSection.Argument("ribbonGroup", ribbonGroup);
        ProfileSection.Argument("window", window);
        ProfileSection.Argument("getImage", getImage);

        try
        {

          // create handler
          var mainWindowButton = (IMainWindowButton)Plugin.CreateInstance(button);


          FrameworkElement ribbonButton;
          ribbonButton = GetRibbonButton(window, getImage, button, ribbonGroup, mainWindowButton);

          Assert.IsNotNull(ribbonButton, "ribbonButton");

          var width = button.GetAttribute("width");
          double d;
          if (!string.IsNullOrEmpty(width) && double.TryParse(width, out d))
          {
            ribbonButton.Width = d;
          }

          // bind IsEnabled event
          if (mainWindowButton != null)
          {
            ribbonButton.Tag = mainWindowButton;
            ribbonButton.IsEnabled = mainWindowButton.IsEnabled(window, SelectedInstance);
            SetIsEnabledProperty(ribbonButton, mainWindowButton);
          }
        }
        catch (Exception ex)
        {
          Log.Error("Plugin Button caused an exception", typeof(MainWindowHelper), ex);
        }
      }
    }

    private static FrameworkElement GetRibbonButton(MainWindow window, Func<string, ImageSource> getImage, XmlElement button, RibbonGroupBox ribbonGroup, IMainWindowButton mainWindowButton)
    {
      var header = button.GetNonEmptyAttribute("label");

      var clickHandler = GetClickHandler(mainWindowButton);

      if (button.ChildNodes.Count == 0)
      {
        // create Ribbon Button
        var imageSource = getImage(button.GetNonEmptyAttribute("largeImage"));
        var fluentButton = new Fluent.Button
        {
          Icon = imageSource,
          LargeIcon = imageSource,
          Header = header
        };
        fluentButton.Click += clickHandler;
        ribbonGroup.Items.Add(fluentButton);
        return fluentButton;
      }

      // create Ribbon Button
      var splitButton = ribbonGroup.Items.OfType<SplitButton>().SingleOrDefault(x => x.Header.ToString().Trim().EqualsIgnoreCase(header.Trim()));
      if (splitButton == null)
      {
        var imageSource = getImage(button.GetNonEmptyAttribute("largeImage"));
        splitButton = new Fluent.SplitButton
        {
          Icon = imageSource,
          LargeIcon = imageSource,
          Header = header
        };

        if (mainWindowButton != null)
        {
          splitButton.Click += clickHandler;
        }
        else
        {
          var childrenButtons = new List<KeyValuePair<string, IMainWindowButton>>();
          splitButton.Tag = childrenButtons;
          splitButton.Click += (sender, args) =>
          {
            IEnumerable<string> options = childrenButtons.Where(x => x.Value.IsEnabled(window, SelectedInstance)).Select(x => x.Key);
            var result = WindowHelper.AskForSelection(header, header, "Choose desired action", options, window, null, null, true);
            if (result == null)
            {
              return;
            }

            var pair = childrenButtons.Single(x => x.Key == result);
            pair.Value.OnClick(window, SelectedInstance);
          };
        }

        ribbonGroup.Items.Add(splitButton);
      }

      var items = splitButton.Items;
      Assert.IsNotNull(items, "items");

      foreach (var menuItem in button.ChildNodes.OfType<XmlElement>())
      {
        try
        {
        var name = menuItem.Name;
        if (name.EqualsIgnoreCase("separator"))
        {
          items.Add(new Separator());
          continue;
        }

        if (!name.EqualsIgnoreCase("button"))
        {
          Log.Error("This element is not supported as SplitButton element: {0}".FormatWith(menuItem.OuterXml), typeof(MainWindowHelper));
          continue;
        }

        var menuHeader = menuItem.GetAttribute("label");
          var largeImage = menuItem.GetAttribute("largeImage");
          var menuIcon = string.IsNullOrEmpty(largeImage) ? null : getImage(largeImage);
        var menuHandler = (IMainWindowButton)Plugin.CreateInstance(menuItem);
        Assert.IsNotNull(menuHandler, "model");

        var childrenButtons = splitButton.Tag as ICollection<KeyValuePair<string, IMainWindowButton>>; 
        if (childrenButtons != null)
        {
          childrenButtons.Add(new KeyValuePair<string, IMainWindowButton>(menuHeader, menuHandler));
        }

        var menuButton = new Fluent.MenuItem()
        {
          Header = menuHeader,
          IsEnabled = menuHandler.IsEnabled(window, SelectedInstance)
        };

          if (menuIcon != null)
          {
            menuButton.Icon = menuIcon;
          }
        // bind IsEnabled event

        SetIsEnabledProperty(menuButton, menuHandler);

        menuButton.Click += delegate
        {
          try
          {
            if (menuHandler.IsEnabled(MainWindow.Instance, SelectedInstance))
            {
              menuHandler.OnClick(MainWindow.Instance, SelectedInstance);
              MainWindowHelper.RefreshInstances();
            }
          }
          catch (Exception ex)
          {
            WindowHelper.HandleError(ex.Message, true, ex);
          }
        };

        items.Add(menuButton);
      }
        catch (Exception ex)
        {
          WindowHelper.HandleError(ex.Message, true, ex);
        }
      }

      return splitButton;
    }

    private static RoutedEventHandler GetClickHandler(IMainWindowButton mainWindowButton)
    {
      var clickHandler = new RoutedEventHandler(delegate
      {
        try
        {
          if (mainWindowButton != null && mainWindowButton.IsEnabled(MainWindow.Instance, SelectedInstance))
          {
            mainWindowButton.OnClick(MainWindow.Instance, SelectedInstance);
            MainWindowHelper.RefreshInstances();
          }
        }
        catch (Exception ex)
        {
          WindowHelper.HandleError(ex.Message, true, ex);
        }
      });

      return clickHandler;
    }

    private static RibbonTabItem CreateTab(MainWindow window, string name)
    {
      var tab = new RibbonTabItem
        {
          Name = "{0}Tab".FormatWith(name.Replace(" ", "_")),
          Header = name
        };

      window.MainRibbon.Tabs.Add(tab);

      return tab;
    }

    private static RibbonGroupBox CreateGroup(RibbonTabItem tab, string name)
    {
      var group = new RibbonGroupBox
        {
          Name = "{0}{1}Group".FormatWith(tab.Name, name.Replace(" ", "_")),
          Header = name
        };

      tab.Groups.Add(group);

      return group;
    }

    private static void InitializeContextMenuItem(XmlElement menuItemElement, ItemCollection itemCollection, MainWindow window, Func<string, ImageSource> getImage)
    {
      try
      {

        if (menuItemElement.Name.EqualsIgnoreCase("separator"))
        {
          itemCollection.Add(new Separator());
          return;
        }

        if (!menuItemElement.Name.EqualsIgnoreCase("item"))
        {
          Assert.IsTrue(false, "The element is not supported: {0}".FormatWith(menuItemElement.OuterXml));
        }

        // create handler
        var mainWindowButton = (IMainWindowButton)Plugin.CreateInstance(menuItemElement);

        // create Context Menu Item
        var menuItem = new MenuItem
          {
            Header = menuItemElement.GetNonEmptyAttribute("header"),
            Icon = new Image
              {
                Source = getImage(menuItemElement.GetNonEmptyAttribute("image")),
                Width = 16,
                Height = 16
              },
            IsEnabled = mainWindowButton == null || mainWindowButton.IsEnabled(window, SelectedInstance),
            Tag = mainWindowButton
          };

        if (mainWindowButton != null)
        {
          menuItem.Click += (obj, e) =>
          {
            try
            {
              if (mainWindowButton.IsEnabled(MainWindow.Instance, SelectedInstance))
              {
                mainWindowButton.OnClick(MainWindow.Instance, SelectedInstance);
                MainWindowHelper.RefreshInstances();
              }
            }
            catch (Exception ex)
            {
              WindowHelper.HandleError(ex.Message, true, ex);
            }
          };

          SetIsEnabledProperty(menuItem, mainWindowButton);
        }

        foreach (var childElement in menuItemElement.ChildNodes.OfType<XmlElement>())
        {
          InitializeContextMenuItem(childElement, menuItem.Items, window, getImage);
        }

        itemCollection.Add(menuItem);


      }
      catch (Exception ex)
      {
        Log.Error("Plugin Menu Item caused an exception", typeof(MainWindowHelper), ex);
      }
    }

    private static void SetIsEnabledProperty(FrameworkElement ribbonButton, IMainWindowButton mainWindowButton)
    {
      ribbonButton.SetBinding(UIElement.IsEnabledProperty, new System.Windows.Data.Binding("SelectedItem")
      {
        Converter = new CustomConverter(mainWindowButton),
        ElementName = "InstanceList"
      });
    }

    private static IEnumerable<XmlElement> SelectNonEmptyCollection(XmlElement xmlElement, string name)
    {
      var collection = xmlElement.SelectElements(name).ToArray();
      Assert.IsTrue(collection.Length > 0, "<{0}> doesn't contain any <{1}> element".FormatWith(xmlElement.Name, name));
      return collection;
    }

    #endregion

    /// <summary>
    ///   Refreshes the instances.
    /// </summary>
    public static void RefreshInstances()
    {
      using (new ProfileSection("Refresh instances", typeof(MainWindowHelper)))
      {
        var mainWindow = MainWindow.Instance;
        var tabIndex = mainWindow.MainRibbon.SelectedTabIndex;
        var instance = SelectedInstance;
        var name = instance != null ? instance.Name : null;
        string instancesFolder = ProfileManager.Profile.InstancesFolder;
        InstanceManager.Initialize(instancesFolder);
        Search();
        if (string.IsNullOrEmpty(name))
        {
          mainWindow.MainRibbon.SelectedTabIndex = tabIndex;
          return;
        }
        var list = mainWindow.InstanceList;
        for (int i = 0; i < list.Items.Count; ++i)
        {
          var item = list.Items[i] as Instance;
          if (item != null && item.Name.EqualsIgnoreCase(name))
          {
            list.SelectedIndex = i;
            mainWindow.MainRibbon.SelectedTabIndex = tabIndex;
            return;
          }
        }
      }
    }

    /// <summary>
    ///   Refreshes the instances with partial cache invalidating. The method is used when we not expect significant IIS changes.
    /// </summary>
    public static void SoftlyRefreshInstances()
    {
      using (new ProfileSection("Refresh instances (softly)", typeof(MainWindowHelper)))
      {
        string instancesFolder = ProfileManager.Profile.InstancesFolder;
        InstanceManager.InitializeWithSoftListRefresh(instancesFolder);
        Search();
      }
    }

    /// <summary>
    ///   Determines whether [is installer ready].
    /// </summary>
    /// <returns> <c>true</c> if [is installer ready]; otherwise, <c>false</c> . </returns>
    public static bool IsInstallerReady()
    {
      try
      {
        ProfileManager.Profile.Validate();
        return true;
      }
      catch (Exception ex)
      {
        Log.Warn("An error occurred during checking if installer ready", typeof(MainWindowHelper), ex);

        return false;
      }
    }

    /// <summary>
    ///   Initializes this instance.
    /// </summary>
    public static void Initialize()
    {
      using (new ProfileSection("Initialize main window", typeof(MainWindowHelper)))
      {
        if (WindowsSettings.AppUiMainWindowWidth.Value > 0)
        {
          double d = WindowsSettings.AppUiMainWindowWidth.Value;
          MainWindow.Instance.MaxWidth = Screen.PrimaryScreen.Bounds.Width;
          MainWindow.Instance.Width = d;
        }

        MainWindowHelper.RefreshInstances();
        PluginManager.ExecuteMainWindowLoadedProcessors(MainWindow.Instance);
        MainWindowHelper.RefreshInstaller();
      }
    }

    public static void InitializeContextMenu(XmlDocumentEx appDocument)
    {
      using (new ProfileSection("Initialize context menu", typeof(MainWindowHelper)))
      {
        ProfileSection.Argument("appDocument", appDocument);

        MainWindow window = MainWindow.Instance;
        var menuItems = appDocument.SelectElements("/app[@version='1.3']/mainWindow/contextMenu/*");

        foreach (var item in menuItems)
        {
          using (new ProfileSection("Fill in context menu", typeof(MainWindowHelper)))
          {
            ProfileSection.Argument("item", item);

            if (item.Name == "item")
            {
              InitializeContextMenuItem(item, window.ContextMenu.Items, window, uri => Plugin.GetImage(uri, "App.xml"));
            }
            else if (item.Name == "separator")
            {
              window.ContextMenu.Items.Add(new Separator());
            }
            else if (item.Name == "plugins")
            {
              using (new ProfileSection("Fill in context menu by plugins", typeof(MainWindowHelper)))
              {
                foreach (var plugin in PluginManager.GetEnabledPlugins())
                {
                  using (new ProfileSection("Fill in context menu by plugin", typeof(MainWindowHelper)))
                  {
                    ProfileSection.Argument("plugin", plugin);

                    try
                    {
                      var pluginMenuItems = plugin.PluginXmlDocument.SelectElements("/plugin[@version='1.3']/mainWindow/contextMenu/item");
                      foreach (var menuItemElement in pluginMenuItems)
                      {
                        InitializeContextMenuItem(menuItemElement, window.ContextMenu.Items, window, plugin.GetImage);
                      }
                    }
                    catch (Exception ex)
                    {
                      PluginManager.HandleError(plugin, ex);
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    public static void InitializeRibbon(XmlDocument appDocument)
    {
      using (new ProfileSection("Initialize main window ribbon", typeof(MainWindowHelper)))
      {
        MainWindow window = MainWindow.Instance;
        using (new ProfileSection("Loading tabs from App.xml", typeof(MainWindowHelper)))
        {
          var tabs = appDocument.SelectElements("/app[@version='1.3']/mainWindow/ribbon/tab");
          foreach (var tabElement in tabs)
          {
            // Get Ribbon Tab to insert button to
            InitializeRibbonTab(tabElement, window, uri => Plugin.GetImage(uri, "App.xml"));
          }
        }

        // load plugins
        using (new ProfileSection("Loading tabs from plugins", typeof(MainWindowHelper)))
        {
          foreach (var plugin in PluginManager.GetEnabledPlugins())
          {
            using (new ProfileSection("Loading tabs from plugin", typeof(MainWindowHelper)))
            {
              ProfileSection.Argument("plugin", plugin);

              try
              {
                var tabs = plugin.PluginXmlDocument.SelectElements("/plugin[@version='1.3']/mainWindow/ribbon/tab");
                foreach (var tabElement in tabs)
                {
                  // Get Ribbon Tab to insert button to
                  InitializeRibbonTab(tabElement, window, plugin.GetImage);
                }
              }
              catch (Exception ex)
              {
                PluginManager.HandleError(plugin, ex);
              }
            }
          }
        }

        // minimize ribbon
        using (new ProfileSection("Normalizing ribbon", typeof(MainWindowHelper)))
        {
          foreach (var tab in window.MainRibbon.Tabs)
          {
            int hiddenGroups = 0;
            foreach (var group in tab.Groups)
            {
              if (group.Items.Count == 0)
              {
                group.Visibility = Visibility.Hidden;
                hiddenGroups += 1;
              }
            }

            if (hiddenGroups == tab.Groups.Count)
            {
              tab.Visibility = Visibility.Hidden;
            }
          }
        }
      }
    }

    public static void RefreshCaches()
    {
      using (new ProfileSection("Refresh caching", typeof(MainWindowHelper)))
      {
        CacheManager.ClearAll();
      }
    }

    public static void RefreshEverything()
    {
      using (new ProfileSection("Refresh everything", typeof(MainWindowHelper)))
      {
        CacheManager.ClearAll();
        MainWindowHelper.RefreshInstaller();
        MainWindowHelper.RefreshInstances();
      }
    }

    public static void UpdateManifests()
    {
      if (!ProductHelper.Settings.CoreManifestsUpdateEnabled.Value || !ManifestHelper.UpdateNeeded)
      {
        return;
      }

      if (ApplicationManager.IsInternal)
      {
        return;
      }

      WindowHelper.LongRunningTask(ManifestHelper.UpdateManifestsSync, "Updating manifests", MainWindow.Instance);
    }

    public static void Publish(Instance instance, Window owner, PublishMode mode)
    {
      WindowHelper.LongRunningTask(
        () => PublishAsync(instance), "Publish",
        owner, "Publish", "Publish 'en' language from 'master' to 'web' with mode " + mode);
    }

    private static void PublishAsync(Instance instance)
    {
      try
      {
        PublishAgentHelper.CopyAgentFiles(instance);
        PublishAgentHelper.Publish(instance);
      }
      catch (ThreadAbortException)
      {
      }
      catch (Exception ex)
      {
        WindowHelper.HandleError("An error occurred while publishing" + Environment.NewLine + ex.Message, true, ex, typeof(MainWindowHelper));
      }
      finally
      {
        AgentHelper.DeleteAgentFiles(instance);
      }
    }
  }
}