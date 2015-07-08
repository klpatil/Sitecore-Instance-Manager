﻿#region Usings

using SIM.Base;
using SIM.Pipelines.Processors;

#endregion

namespace SIM.Pipelines.Install
{
  #region

  

  #endregion

  /// <summary>
  ///   The install processor.
  /// </summary>
  public abstract class InstallProcessor : Processor
  {
    #region Public Methods

    /// <summary>
    /// The is require processing.
    /// </summary>
    /// <param name="args">
    /// The args. 
    /// </param>
    /// <returns>
    /// The is require processing. 
    /// </returns>
    public override sealed bool IsRequireProcessing(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      return this.IsRequireProcessing((InstallArgs)args);
    }

    #endregion

    #region Methods

    /// <summary>
    /// The is require processing.
    /// </summary>
    /// <param name="args">
    /// The args. 
    /// </param>
    /// <returns>
    /// The is require processing. 
    /// </returns>
    protected virtual bool IsRequireProcessing([NotNull] InstallArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      return true;
    }

    /// <summary>
    /// The process.
    /// </summary>
    /// <param name="args">
    /// The args. 
    /// </param>
    protected override sealed void Process([NotNull] ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      this.Process((InstallArgs)args);
    }

    /// <summary>
    /// The process.
    /// </summary>
    /// <param name="args">
    /// The args. 
    /// </param>
    protected abstract void Process([NotNull] InstallArgs args);

    #endregion
  }
}