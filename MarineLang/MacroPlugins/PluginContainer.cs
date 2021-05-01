using System.Collections.Generic;

namespace MarineLang.MacroPlugins
{
    public class PluginContainer
    {
        private readonly Dictionary<string, IFuncDefinitionMacroPlugin> funcDefinitionMacroPlugins
            = new Dictionary<string, IFuncDefinitionMacroPlugin>();

        public PluginContainer()
        {

        }

        public void AddFuncDefinitionPlugin(string pluginName,IFuncDefinitionMacroPlugin funcDefinitionMacroPlugin)
        {
            funcDefinitionMacroPlugins.Add(pluginName, funcDefinitionMacroPlugin);
        }

        public IFuncDefinitionMacroPlugin GetFuncDefinitionPlugin(string pluginName)
        {
            return funcDefinitionMacroPlugins[pluginName];
        }
    }
}
