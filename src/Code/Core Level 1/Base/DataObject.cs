﻿#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

#endregion

namespace SIM.Base
{
  #region

  

  #endregion

  /// <summary>
  ///   The data object.
  /// </summary>
  [Serializable]
  public class DataObject : DataObjectBase
  {
    #region Fields

    /// <summary>
    ///   The values.
    /// </summary>
    private readonly Dictionary<string, object> values = new Dictionary<string, object>();

    #endregion

    #region Public Methods

    /// <summary>
    /// The load.
    /// </summary>
    /// <param name="nodes">
    /// The nodes. 
    /// </param>
    public void Load([NotNull] XmlNode[] nodes)
    {
      Assert.ArgumentNotNull(nodes, "nodes");

      foreach (XmlElement element in nodes.OfType<XmlElement>())
      {
        this.values.Add(element.Name, element.InnerXml);
      }
    }

    /// <summary>
    /// The save.
    /// </summary>
    /// <param name="root">
    /// The root. 
    /// </param>
    public void Save([NotNull] XmlElement root)
    {
      Assert.ArgumentNotNull(root, "root");

      XmlDocument document = root.OwnerDocument;
      Assert.IsNotNull(document, "Root element must have a document");
      foreach (string key in this.values.Keys)
      {
        XmlElement element = document.CreateElement(key);
        element.InnerXml = this.values[key].ToString();
        root.AppendChild(element);
      }
    }

    /// <summary>
    ///   The validate.
    /// </summary>
    public virtual void Validate()
    {
      Type type = this.GetType();
      const string validatePrefix = "Validate";
      IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(pr => pr.Name.StartsWith(validatePrefix) && pr.Name.Length != validatePrefix.Length);
      foreach (MethodInfo method in methods)
      {
        method.Invoke(this, new object[0]);
      }
    }

    #endregion

    #region Methods

    /// <summary>
    /// The get string.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <returns>
    /// The get string. 
    /// </returns>
    [CanBeNull]
    protected string GetString([NotNull] string name)
    {
      Assert.ArgumentNotNull(name, "name");

      return this.GetValue(name) as string;
    }

    /// <summary>
    /// The get value.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <returns>
    /// The get value. 
    /// </returns>
    [CanBeNull]
    protected object GetValue([NotNull] string name)
    {
      Assert.ArgumentNotNull(name, "name");

      return this.values.ContainsKey(name) ? this.values[name] : null;
    }

    /// <summary>
    /// The set value.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <param name="value">
    /// The value. 
    /// </param>
    protected virtual void SetValue([NotNull] string name, [CanBeNull] object value)
    {
      Assert.ArgumentNotNull(name, "name");

      this.values[name] = value;
      this.NotifyPropertyChanged(name);
    }

    /// <summary>
    /// The set value.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <param name="value">
    /// The value. 
    /// </param>
    protected void SetValue([NotNull] string name, [CanBeNull] string value)
    {
      Assert.ArgumentNotNull(name, "name");

      this.SetValue(name, value as object);
    }

    #endregion
  }
}