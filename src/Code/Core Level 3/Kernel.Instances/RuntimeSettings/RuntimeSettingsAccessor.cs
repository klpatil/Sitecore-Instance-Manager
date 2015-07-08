﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SIM.Adapters.SqlServer;
using SIM.Adapters.WebServer;
using SIM.Base;
using Sitecore.ConfigBuilder;

namespace SIM.Instances.RuntimeSettings
{
  using SIM.Adapters.MongoDb;

  public class RuntimeSettingsAccessor
  {
    protected Instance Instance { get; set; }

    protected virtual string WebConfigPath
    {
      get
      {
        try
        {
          return WebConfig.GetWebConfigPath(Instance.WebRootPath);
        }
        catch (Exception ex)
        {
          throw new InvalidOperationException(string.Format("Failed to get web config path of {0}", Instance.WebRootPath), ex);
        }
      }
    }

    public RuntimeSettingsAccessor(Instance instance)
    {
      this.Instance = instance;
    }

    public virtual XmlDocument GetShowconfig(bool normalize = false)
    {
      using (new ProfileSection("Computing showconfig", this))
      {
        try
        {
          ProfileSection.Argument("normalize", normalize);

          var showConfig = ConfigBuilder.Build(WebConfigPath, false, normalize);

          return ProfileSection.Result(showConfig);
        }
        catch (Exception ex)
        {
          throw new InvalidOperationException(string.Format("Failed to get showconfig of {0}", Instance.WebRootPath), ex);
        }
      }
    }

    public virtual XmlDocument GetWebConfigResult(bool normalize = false)
    {
      using (new ProfileSection("Computing web config result", this))
      {
        try
        {
          ProfileSection.Argument("normalize", normalize);

          var webConfigResult = ConfigBuilder.Build(WebConfigPath, true, normalize);
          
          return ProfileSection.Result(webConfigResult); 
        }
        catch (Exception ex)
        {
          throw new InvalidOperationException(string.Format("Failed to get web config result of {0}", Instance.WebRootPath), ex);
        }
      }
    }

    public virtual string GetScVariableValue([NotNull] string variableName)
    {
      try
      {
        var webConfigResult = this.GetWebConfigResult();
        return WebConfig.GetScVariable(webConfigResult, variableName);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(string.Format("Failed to get {1} sc variable of {0}", Instance.WebRootPath, variableName), ex);
      }
    }

    public virtual string GetSitecoreSettingValue(string name)
    {
      try
      {
        var webConfigResult = this.GetWebConfigResult();
        return WebConfig.GetSitecoreSetting(name, webConfigResult);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(string.Format("Failed to get {1} sitecore setting of {0}", Instance.WebRootPath, name), ex);
      }
    }

    public virtual ICollection<Database> GetDatabases()
    {
      try
      {
        var webConfigDocument = this.GetWebConfigResult();
        var webRootPath = Instance.WebRootPath;
        return WebConfig.GetDatabases(webRootPath, webConfigDocument);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(string.Format("Failed to get databases of {0}", Instance.WebRootPath), ex);
      }
    }

    public virtual ICollection<MongoDbDatabase> GetMongoDatabases()
    {
      try
      {
        var webConfigDocument = this.GetWebConfigResult();
        var webRootPath = Instance.WebRootPath;
        return WebConfig.GetMongoDatabases(webRootPath, webConfigDocument);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(string.Format("Failed to get mongo databases of {0}", Instance.WebRootPath), ex);
      }
    }

    public virtual XmlElement GetScVariableElement([NotNull] string elementName)
    {
      try
      {
        var webConfigResult = this.GetWebConfigResult();
        return WebConfig.GetScVariableElement(webConfigResult, elementName);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException(string.Format("Failed to get {1} sc variable of {0}", Instance.WebRootPath, elementName), ex);
      }
    }
  }
}
