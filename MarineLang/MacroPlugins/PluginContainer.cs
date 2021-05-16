using System.Collections.Generic;

namespace MarineLang.MacroPlugins
{
    public class PluginContainer
    {
        private readonly Dictionary<string, IFuncDefinitionMacroPlugin> funcDefinitionMacroPlugins
            = new Dictionary<string, IFuncDefinitionMacroPlugin>();

        private readonly Dictionary<string, IExprMacroPlugin> exprMacroPlugins
            = new Dictionary<string, IExprMacroPlugin>();

        public void AddFuncDefinitionPlugin(string pluginName,IFuncDefinitionMacroPlugin funcDefinitionMacroPlugin)
        {
            funcDefinitionMacroPlugins.Add(pluginName, funcDefinitionMacroPlugin);
        }

        public void AddExprPlugin(string pluginName, IExprMacroPlugin exprMacroPlugin)
        {
            exprMacroPlugins.Add(pluginName, exprMacroPlugin);
        }

        public IFuncDefinitionMacroPlugin GetFuncDefinitionPlugin(string pluginName)
        {
            return funcDefinitionMacroPlugins[pluginName];
        }

        public IExprMacroPlugin GetExprPlugin(string pluginName)
        {
            return exprMacroPlugins[pluginName];
        }
    }
}
