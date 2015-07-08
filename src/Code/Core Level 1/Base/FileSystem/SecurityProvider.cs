using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SIM.Base
{
  public static class SecurityExtensions
  {
    static readonly Type SecurityIdentifier = typeof(SecurityIdentifier);
    public static bool CompareTo(this IdentityReference left, IdentityReference right)
    {
      return left != null && right != null
        ? left.Translate(SecurityIdentifier).ToString().EqualsIgnoreCase(right.Translate(SecurityIdentifier).ToString())
        : left == right;
    }
  }

  public class SecurityProvider
  {
    /// <summary>
    ///   The authenticated users identity.
    /// </summary>
    protected readonly IdentityReference AuthenticatedUsers = new SecurityIdentifier("S-1-5-11").Translate(typeof(NTAccount));

    /// <summary>
    ///   The network service identity.
    /// </summary>
    protected readonly IdentityReference NetworkService = new SecurityIdentifier("S-1-5-20").Translate(typeof(NTAccount));

    /// <summary>
    ///   The local system identity.
    /// </summary>
    protected readonly IdentityReference LocalSystem = new SecurityIdentifier("S-1-5-18").Translate(typeof(NTAccount));

    /// <summary>
    ///   The local service identity.
    /// </summary>
    protected readonly IdentityReference LocalService = new SecurityIdentifier("S-1-5-19").Translate(typeof(NTAccount));

    /// <summary>
    ///   The local service identity.
    /// </summary>
    protected readonly IdentityReference Everyone = new SecurityIdentifier("S-1-1-0").Translate(typeof(NTAccount));

    protected readonly FileSystem fileSystem;

    public SecurityProvider(FileSystem fileSystem)
    {
      this.fileSystem = fileSystem;
    }

    /// <summary>
    /// The get rules.
    /// </summary>
    /// <param name="rules">
    /// The rules. 
    /// </param>
    /// <param name="identity">
    /// The identity. 
    /// </param>
    /// <returns>
    /// </returns>
    [NotNull]
    protected virtual IEnumerable<AuthorizationRule> GetRules([NotNull] AuthorizationRuleCollection rules,
                                                           [NotNull] IdentityReference identity)
    {
      Assert.ArgumentNotNull(rules, "rules");
      Assert.ArgumentNotNull(identity, "identity");

      try
      {
        return rules.Cast<AuthorizationRule>().Where(rule => rule.IdentityReference.CompareTo(identity) || rule.IdentityReference.CompareTo(Everyone));
      }
      catch (Exception ex)
      {
        Log.Warn("Cannot get rules. {0}".FormatWith(ex.Message), typeof(FileSystem), ex);
        return new AuthorizationRule[0];
      }
    }

    protected virtual bool HasDirectoryPermissions(string path, IdentityReference identity, FileSystemRights permissions)
    {
      DirectoryInfo dirInfo = new DirectoryInfo(path);
      DirectorySecurity dirSecurity = dirInfo.GetAccessControl(AccessControlSections.All);
      AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

      return HasPermissions(rules, identity, permissions);
    }

    protected virtual bool HasFilePermissions(string path, IdentityReference identity, FileSystemRights permissions)
    {
      var dirInfo = new FileInfo(path);
      var dirSecurity = dirInfo.GetAccessControl(AccessControlSections.All);
      AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

      return HasPermissions(rules, identity, permissions);
    }

    /// <summary>
    /// The has permissions.
    /// </summary>
    /// <param name="rules">
    ///   The rules. 
    /// </param>
    /// <param name="identity">
    ///   The identity. 
    /// </param>
    /// <param name="permissions"></param>
    /// <returns>
    /// The has permissions. 
    /// </returns>
    protected virtual bool HasPermissions([NotNull] AuthorizationRuleCollection rules, [NotNull] IdentityReference identity, FileSystemRights permissions)
    {
      Assert.ArgumentNotNull(rules, "rules");
      Assert.ArgumentNotNull(identity, "identity");
      try
      {
        return
          GetRules(rules, identity).Any(
            rule => (((FileSystemAccessRule)rule).FileSystemRights & permissions) > 0);
      }
      catch (Exception ex)
      {
        Log.Warn("Cannot get permissions for rules collection", typeof(FileSystem), ex);
        return false;
      }
    }

    /// <summary>
    /// The get identity reference.
    /// </summary>
    /// <param name="name">
    /// The name. 
    /// </param>
    /// <returns>
    /// </returns>
    [CanBeNull]
    public virtual IdentityReference GetIdentityReference([NotNull] string name)
    {
      Assert.ArgumentNotNullOrEmpty(name, "name");

      IdentityReference reference = null;
      if (name.EndsWith("NetworkService", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Network Service", StringComparison.OrdinalIgnoreCase))
      {
        reference = this.NetworkService;
      }
      else if (name.EndsWith("LocalSystem", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Local System", StringComparison.OrdinalIgnoreCase))
      {
        reference = this.LocalSystem;
      }
      else if (name.EndsWith("LocalService", StringComparison.OrdinalIgnoreCase) || name.EndsWith("Local Service", StringComparison.OrdinalIgnoreCase))
      {
        reference = this.LocalService;
      }
      else 
      {
        try
        {
          if (!name.Contains("\\"))
          {
            name = Environment.MachineName + "\\" + name.TrimStart("\\");
          }
          else if (name.StartsWith(".\\"))
          {
            name = Environment.MachineName + "\\" + name.TrimStart(".\\");
          }

          reference = new SecurityIdentifier(name).Translate(typeof(NTAccount));
        }
        catch (Exception ex)
        {
          Log.Warn("An error occurred during paring {0} security identifier".FormatWith(name), this, ex);
          try
          {
            reference = new NTAccount(name);
          }
          catch (Exception ex1)
          {
            Log.Warn("An error occurred during parsing {0} user account", this, ex1);
          }
        }
      }

      Assert.IsNotNull(reference, "The '" + name + "' isn't valid NTAccount");

      return reference;
    }

    public virtual void EnsurePermissions([NotNull] string path, [NotNull] string identity)
    {
      Assert.ArgumentNotNullOrEmpty(path, "path");
      Assert.ArgumentNotNullOrEmpty(identity, "identity");

      var identityReference = GetIdentityReference(identity);
      Assert.IsNotNull(identityReference, "Cannot find {0} identity reference".FormatWith(identity));

      if (fileSystem.Directory.Exists(path))
      {
        EnsureDirectoryPermissions(path, identityReference);
        return;
      }

      if (fileSystem.File.Exists(path))
      {
        EnsureFilePermissions(path, identityReference);
        return;
      }

      throw new InvalidOperationException("File or directory not found: " + path);
    }

    /// <summary>
    /// The ensure permissions.
    /// </summary>
    /// <param name="path">
    /// The path. 
    /// </param>
    /// <param name="identity">
    /// The identity. 
    /// </param>
    protected virtual void EnsureDirectoryPermissions([NotNull] string path, [NotNull] IdentityReference identity)
    {
      Assert.ArgumentNotNull(path, "path");
      Assert.ArgumentNotNull(identity, "identity");

      DirectoryInfo dirInfo = new DirectoryInfo(path);
      DirectorySecurity dirSecurity = dirInfo.GetAccessControl(AccessControlSections.All);
      AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

      if (!HasPermissions(rules, identity, FileSystemRights.FullControl))
      {
        Log.Info("Granting full access for '{0}' identity to the '{1}' folder".FormatWith(identity, path),
          typeof(FileSystem));
        FileSystemAccessRule rule = new FileSystemAccessRule(identity, FileSystemRights.FullControl,
          InheritanceFlags.ContainerInherit |
          InheritanceFlags.ObjectInherit, PropagationFlags.None,
          AccessControlType.Allow);
        dirSecurity.AddAccessRule(rule);
        dirInfo.SetAccessControl(dirSecurity);

        dirSecurity = dirInfo.GetAccessControl(AccessControlSections.All);
        rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));
        Assert.IsTrue(HasPermissions(rules, identity, FileSystemRights.FullControl),
          "The Full Control access to the '" + path + "' folder isn't permitted for " + identity.Value +
          ". Please fix it and then restart the process");
      }
    }

    /// <summary>
    /// The ensure permissions.
    /// </summary>
    /// <param name="path">
    /// The path. 
    /// </param>
    /// <param name="identity">
    /// The identity. 
    /// </param>
    protected virtual void EnsureFilePermissions([NotNull] string path, [NotNull] IdentityReference identity)
    {
      Assert.ArgumentNotNull(path, "path");
      Assert.ArgumentNotNull(identity, "identity");

      var fileInfo = new FileInfo(path);
      var dirSecurity = fileInfo.GetAccessControl(AccessControlSections.All);
      AuthorizationRuleCollection rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

      if (!HasPermissions(rules, identity, FileSystemRights.FullControl))
      {
        Log.Info("Granting full access for '{0}' identity to the '{1}' file".FormatWith(identity, path),
          typeof(FileSystem));

        var rule = new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow);
          dirSecurity.AddAccessRule(rule);
        fileInfo.SetAccessControl(dirSecurity);

        dirSecurity = fileInfo.GetAccessControl(AccessControlSections.All);
        rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));
        Assert.IsTrue(HasPermissions(rules, identity, FileSystemRights.FullControl),
          "The Full Control access to the '" + path + "' file isn't permitted for " + identity.Value +
          ". Please fix it and then restart the process");
      }
    }

    public virtual bool HasPermissions(string path, string identity, FileSystemRights permissions)
    {
      Assert.ArgumentNotNullOrEmpty(path, "path");
      Assert.ArgumentNotNullOrEmpty(identity, "identity");
      Assert.ArgumentNotNull(permissions, "permissions");

      if (fileSystem.Directory.Exists(path))
      {
        return HasDirectoryPermissions(path, GetIdentityReference(identity), permissions);
      }

      if (fileSystem.File.Exists(path))
      {
        return HasFilePermissions(path, GetIdentityReference(identity), permissions);
      }

      throw new InvalidOperationException("File or directory not found: " + path);
    }
  }
}