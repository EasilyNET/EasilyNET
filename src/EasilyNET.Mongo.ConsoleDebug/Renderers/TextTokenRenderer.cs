using EasilyNET.Mongo.ConsoleDebug.Attributes;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EasilyNET.Mongo.ConsoleDebug.Renderers;

internal sealed class TextTokenRenderer(string text) : ITemplateTokenRenderer
{
    public IEnumerable<IRenderable> Render(LogEvent logEvent)
    {
        yield return new Text(text, Spectre.Console.Style.Plain);
    }
}