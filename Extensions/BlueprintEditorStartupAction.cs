using System;
using BlueprintEditorPlugin.Utils;
using Frosty.Core;
using FrostySdk.Interfaces;

namespace BlueprintEditorPlugin.Extensions
{
    public class BlueprintEditorStartupAction : StartupAction
    {
        public override Action<ILogger> Action => action;

        private void action(ILogger logger)
        {
            logger.Log("Initializing blueprint editor utilities...");
            NodeUtils.Initialize(logger);
            EditorUtils.Initialize(logger);
        }
    }
}