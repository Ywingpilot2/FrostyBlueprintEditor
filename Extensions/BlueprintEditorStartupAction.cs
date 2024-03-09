using System;
using BlueprintEditorPlugin.Options;
using Frosty.Core;
using FrostySdk.Interfaces;

namespace BlueprintEditorPlugin.Extensions
{
    public class BlueprintEditorStartupAction : StartupAction
    {
        public override Action<ILogger> Action => action;

        private void action(ILogger logger)
        {
            logger.Log("Updating Blueprint Editor options...");
            EditorOptions.Update();
        }
    }
}