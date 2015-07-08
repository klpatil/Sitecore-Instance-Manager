﻿#region Usings

using Microsoft.Web.Administration;
using SIM.Base;
using SIM.Instances;

#endregion

namespace SIM.Pipelines.Install
{
  


  #region

  using SIM.Adapters.WebServer;

  #endregion

  /// <summary>
  ///   The setup website.
  /// </summary>
  [UsedImplicitly]
  public class SetupWebsite : InstallProcessor
  {
    #region Methods

    #region Protected methods

    /// <summary>
    /// The process.
    /// </summary>
    /// <param name="args">
    /// The args. 
    /// </param>
    protected override void Process([NotNull] InstallArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      string name = args.Name;
      string hostName = args.HostName;
      string webRootPath = args.WebRootPath;
      bool enable32BitAppOnWin64 = args.Is32Bit;
      bool forceNetFramework4 = args.ForceNetFramework4;
      bool isClassic = args.IsClassic;
      var id = SetupWebsiteHelper.SetupWebsite(enable32BitAppOnWin64, webRootPath, forceNetFramework4, isClassic, new [] { new BindingInfo("http", hostName, 80, "*"), }, name);
      args.Instance = InstanceManager.GetInstance(id);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// The choose app pool name.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <param name="applicationPools">
    /// The application pools. 
    /// </param>
    /// <returns>
    /// The choose app pool name. 
    /// </returns>
    [NotNull]
    private static string ChooseAppPoolName([NotNull] string name, [NotNull] ApplicationPoolCollection applicationPools)
    {
      Assert.ArgumentNotNull(name, "name");
      Assert.ArgumentNotNull(applicationPools, "applicationPools");

      int modifier = 0;
      string newname = name;
      while (applicationPools[newname] != null)
      {
        newname = name + '_' + ++modifier;
      }

      return newname;
    }

    /// <summary>
    /// The get identity type.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <returns>
    /// </returns>
    private ProcessModelIdentityType GetIdentityType([NotNull] string name)
    {
      Assert.ArgumentNotNull(name, "name");

      if (name.EqualsIgnoreCase("NetworkService"))
      {
        return ProcessModelIdentityType.NetworkService;
      }

      if (name.EqualsIgnoreCase("ApplicationPoolIdentity"))
      {
        return ProcessModelIdentityType.ApplicationPoolIdentity;
      }

      if (name.EqualsIgnoreCase("LocalService"))
      {
        return ProcessModelIdentityType.LocalService;
      }

      if (name.EqualsIgnoreCase("LocalSystem"))
      {
        return ProcessModelIdentityType.LocalSystem;
      }

      return ProcessModelIdentityType.SpecificUser;
    }

    #endregion

    #endregion
  }
}