﻿using System.Windows;
using SIM.Instances;
using SIM.Tool.Base;
using SIM.Tool.Base.Plugins;

namespace SIM.Tool.Windows.MainWindowComponents
{
  using System;
  using System.Linq;
  using SIM.Base;

  public class BrowseButton : IMainWindowButton
  {
    [NotNull]
    protected readonly string VirtualPath;

    [CanBeNull]
    protected readonly string Browser;

    [NotNull]
    private string[] Params;

    public BrowseButton()
    {
      this.VirtualPath = string.Empty;
      this.Browser = null;
      this.Params = new string[0];
    }

    public BrowseButton(string param)
    {
      var arr = (param + ":").Split(':');
      this.VirtualPath = arr[0];
      this.Browser = arr[1];
      this.Params = arr.Skip(2).ToArray();
    }

    public bool IsEnabled(Window mainWindow, Instance instance)
    {
      return instance != null;
    }

    public void OnClick(Window mainWindow, Instance instance)
    {
      if (instance != null)
      {
        if (!InstanceHelperEx.PreheatInstance(instance, mainWindow))
        {
          return;
        }

        InstanceHelperEx.BrowseInstance(instance, mainWindow, this.VirtualPath, true, this.Browser, this.Params);
      }
    }
  }
}
