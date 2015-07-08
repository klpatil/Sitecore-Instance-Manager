﻿namespace SIM.Pipelines.Delete
{
  #region

  using System;
  using Microsoft.Win32;
  using SIM.Base;

  #endregion

  /// <summary>
  ///   The delete website.
  /// </summary>
  [UsedImplicitly]
  public class DeleteRegistryKey : DeleteProcessor
  {
    protected const string SitecoreNodePath = "SOFTWARE\\Wow6432Node\\Sitecore CMS";
    
    #region Methods

    /// <summary>
    /// The process.
    /// </summary>
    /// <param name="args">
    /// The args. 
    /// </param>
    protected override void Process(DeleteArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var localMachine = Registry.LocalMachine;
      Assert.IsNotNull(localMachine, "localMachine");

      var sitecoreNode = localMachine.OpenSubKey(SitecoreNodePath, true);
      if (sitecoreNode == null)
      {
        return;
      }

      foreach (var subKeyName in sitecoreNode.GetSubKeyNames())
      {
        Assert.IsNotNull(subKeyName, "subKeyName");

        var instanceNode = sitecoreNode.OpenSubKey(subKeyName);
        if (instanceNode == null)
        {
          continue;
        }

        var name = instanceNode.GetValue("IISSiteName") as string ?? string.Empty;
        var dir = (instanceNode.GetValue("InstanceDirectory") as string ?? string.Empty).TrimEnd('\\');
        if (name.Equals(args.InstanceName, StringComparison.OrdinalIgnoreCase) || dir.Equals(args.Instance.RootPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
          Log.Info(string.Format("Deleting {0}\\{1} key from registry", SitecoreNodePath, subKeyName), this);
          sitecoreNode.DeleteSubKey(subKeyName);

          return;
        }
      }
    }

    #endregion
  }
}